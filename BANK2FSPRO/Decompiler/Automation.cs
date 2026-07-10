using System.Xml.Linq;
using FModBankParser.Enums;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Effects;
using FModBankParser.Nodes.Instruments;
using FModBankParser.Nodes.ModulatorSubnodes;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler {
    private void AppendOwnerAutomationAndModulators(
        List<object> documentObjects,
        Guid ownerGuid,
        EventNode eventNode,
        Guid timelineGuid,
        List<object> ownerContent,
        List<Guid>? automationTrackGuids = null
    ) {
        List<Guid> automatorGuids = [];
        List<Guid> modulatorGuids = [];

        foreach ((Guid controllerGuid, ControllerNode controller) in _collectedBank.ControllerNodes) {
            if (controller.PropertyOwnerGuid.ToGuid() != ownerGuid) { continue; }
            if (!TryBuildAutomatorObjects(controllerGuid, controller, eventNode, timelineGuid, out XElement automatorObject, out List<XElement> curveObjects)) {
                continue;
            }
            automatorGuids.Add(controllerGuid);
            documentObjects.Add(automatorObject);
            documentObjects.AddRange(curveObjects);

            if (automationTrackGuids is not null) {
                Guid trackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{controllerGuid}/AutomationTrack");
                automationTrackGuids.Add(trackGuid);
                documentObjects.Add(XmlBuilder.Object("AutomationTrack", trackGuid,
                    XmlBuilder.Relationship("automator", controllerGuid)
                ));
            }
        }

        foreach ((Guid modulatorGuid, ModulatorNode modulator) in _collectedBank.ModulatorNodes) {
            if (modulator.OwnerGuid.ToGuid() != ownerGuid) { continue; }
            if (!TryBuildModulatorObject(modulatorGuid, modulator, out XElement modulatorObject)) { continue; }
            modulatorGuids.Add(modulatorGuid);
            documentObjects.Add(modulatorObject);
        }

        if (_collectedBank.InstrumentNodes.TryGetValue(ownerGuid, out BaseInstrumentNode? instrument) &&
            instrument.InstrumentBody is not null &&
            ShouldEmitAutopitch(instrument.InstrumentBody)) {
            Guid autopitchGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{ownerGuid}/AutopitchModulator");
            modulatorGuids.Add(autopitchGuid);
            List<object> autopitchContent = [
                XmlBuilder.Property("nameOfPropertyBeingModulated", "pitch"),
                XmlBuilder.Property("root", instrument.InstrumentBody.AutoPitchReference),
                XmlBuilder.Property("valueAtMin", instrument.InstrumentBody.AutoPitchAtMinimum),
            ];
            documentObjects.Add(XmlBuilder.Object("AutopitchModulator", autopitchGuid, autopitchContent.ToArray()));
        }

        if (automatorGuids.Count > 0) {
            ownerContent.Add(XmlBuilder.Relationship("automators", automatorGuids));
        }
        if (modulatorGuids.Count > 0) {
            ownerContent.Add(XmlBuilder.Relationship("modulators", modulatorGuids));
        }
    }

    private bool TryBuildAutomatorObjects(
        Guid controllerGuid,
        ControllerNode controller,
        EventNode eventNode,
        Guid timelineGuid,
        out XElement automatorObject,
        out List<XElement> curveObjects
    ) {
        automatorObject = null!;
        curveObjects = [];

        if (!_collectedBank.CurveNodes.TryGetValue(controller.CurveGuid.ToGuid(), out CurveNode? curve)) {
            return false;
        }
        if (curve.CurvePoints.Length == 0) { return false; }
        if (IsDegenerateCurve(curve)) { return false; }

        string propertyName = ResolvePropertyName(controller.PropertyIndex, controller.PropertyOwnerGuid.ToGuid());
        Guid parameterGuid = ResolveAutomationParameterGuid(curve, eventNode, timelineGuid);
        Guid curveGuid = controller.CurveGuid.ToGuid() == controllerGuid
            ? Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{controllerGuid}/AutomationCurve")
            : controller.CurveGuid.ToGuid();

        List<Guid> pointGuids = [];
        for (int i = 0; i < curve.CurvePoints.Length; i++) {
            FCurvePoint point = curve.CurvePoints[i];
            Guid pointGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{curveGuid}/point/{i}");
            pointGuids.Add(pointGuid);

            List<object> pointContent = [
                XmlBuilder.Property("position", point.X),
                XmlBuilder.Property("value", point.Y),
                XmlBuilder.Property("curveShape", point.Shape),
                XmlBuilder.Property("isSCurve", point.Shape != 0),
            ];
            curveObjects.Add(XmlBuilder.Object("AutomationPoint", pointGuid, pointContent.ToArray()));
        }

        curveObjects.Insert(0, XmlBuilder.Object("AutomationCurve", curveGuid,
            XmlBuilder.Relationship("parameter", parameterGuid),
            XmlBuilder.Relationship("automationPoints", pointGuids)
        ));

        automatorObject = XmlBuilder.Object("Automator", controllerGuid,
            XmlBuilder.Property("nameOfPropertyBeingAutomated", propertyName),
            XmlBuilder.Relationship("automationCurves", curveGuid)
        );
        return true;
    }

    private bool TryBuildModulatorObject(Guid modulatorGuid, ModulatorNode modulator, out XElement modulatorObject) {
        modulatorObject = null!;
        string propertyName = ResolvePropertyName(modulator.PropertyIndex, modulator.OwnerGuid.ToGuid(), modulator.PropertyType);

        switch (modulator.Subnode) {
            case ADSRModulatorNode adsr: {
                List<object> content = [
                    XmlBuilder.Property("nameOfPropertyBeingModulated", propertyName),
                    XmlBuilder.Property("initialValue", adsr.InitialValue),
                    XmlBuilder.Property("peakValue", adsr.PeakValue),
                    XmlBuilder.Property("sustainValue", adsr.SustainValue),
                    XmlBuilder.Property("attackTime", adsr.AttackTime),
                    XmlBuilder.Property("holdTime", adsr.HoldTime),
                    XmlBuilder.Property("decayTime", adsr.DecayTime),
                    XmlBuilder.Property("releaseTime", adsr.ReleaseTime),
                    XmlBuilder.Property("attackShape", adsr.AttackShape),
                    XmlBuilder.Property("decayShape", adsr.DecayShape),
                    XmlBuilder.Property("releaseShape", adsr.ReleaseShape),
                ];
                if (adsr.FinalValue is not null) {
                    content.Add(XmlBuilder.Property("finalValue", adsr.FinalValue.Value));
                }
                modulatorObject = XmlBuilder.Object("ADSRModulator", modulatorGuid, content.ToArray());
                return true;
            }

            case RandomModulatorNode random: {
                List<object> content = [
                    XmlBuilder.Property("nameOfPropertyBeingModulated", propertyName),
                    XmlBuilder.Property("amount", random.Amount),
                ];
                modulatorObject = XmlBuilder.Object("RandomizerModulator", modulatorGuid, content.ToArray());
                return true;
            }

            case LFOModulatorNode lfo: {
                List<object> content = [
                    XmlBuilder.Property("nameOfPropertyBeingModulated", propertyName),
                    XmlBuilder.Property("rate", lfo.Rate),
                    XmlBuilder.Property("amount", lfo.Amount),
                    XmlBuilder.Property("phase", lfo.Phase),
                    XmlBuilder.Property("shape", lfo.Shape),
                ];
                modulatorObject = XmlBuilder.Object("LFOModulator", modulatorGuid, content.ToArray());
                return true;
            }

            case SeekModulatorNode seek: {
                List<object> content = [
                    XmlBuilder.Property("nameOfPropertyBeingModulated", propertyName),
                    XmlBuilder.Property("seekSpeed", seek.SeekSpeedAscending),
                ];
                modulatorObject = XmlBuilder.Object("SeekModulator", modulatorGuid, content.ToArray());
                return true;
            }

            case EnvelopeModulatorNode envelope: {
                List<object> content = [
                    XmlBuilder.Property("nameOfPropertyBeingModulated", propertyName),
                ];
                if (envelope.Amount is not null) {
                    content.Add(XmlBuilder.Property("amount", envelope.Amount.Value));
                }
                modulatorObject = XmlBuilder.Object("SidechainModulator", modulatorGuid, content.ToArray());
                return true;
            }

            default:
                return false;
        }
    }

    private static bool ShouldEmitAutopitch(InstrumentNode body) {
        return !body.ControlParameterGuid.IsEmpty || body.AutoPitchAtMinimum != 0;
    }

    private static bool IsDegenerateCurve(CurveNode curve) {
        float maxAbsX = 0;
        foreach (FCurvePoint point in curve.CurvePoints) {
            maxAbsX = Math.Max(maxAbsX, Math.Abs(point.X));
        }
        return maxAbsX < 1e-20f;
    }

    private string ResolvePropertyName(int propertyIndex, Guid ownerGuid, EPropertyType propertyType = EPropertyType.PropertyType_Normal) {
        if (propertyType == EPropertyType.PropertyType_Volume || propertyIndex == 0) {
            return "volume";
        }
        if (propertyIndex == 1) { return "pitch"; }

        if (_collectedBank.EffectNodes.TryGetValue(ownerGuid, out BaseEffectNode? effect)) {
            return propertyIndex switch {
                1000 => "dryLevel",
                1001 => "wetMix",
                1002 => effect is BuiltInEffectNode ? "wetLevel" : "level",
                _ => $"property{propertyIndex}",
            };
        }

        return propertyIndex switch {
            4 => "start",
            1000 => "dryLevel",
            1001 => "wetMix",
            1002 => "wetLevel",
            _ => $"property{propertyIndex}",
        };
    }

    private Guid ResolveAutomationParameterGuid(CurveNode curve, EventNode eventNode, Guid timelineGuid) {
        float minX = curve.CurvePoints.Min(p => p.X);
        float maxX = curve.CurvePoints.Max(p => p.X);

        Guid bestParam = Guid.Empty;
        float bestScore = float.MaxValue;

        foreach (FModGuid layoutRef in eventNode.ParameterLayouts) {
            if (!_collectedBank.ParameterLayoutNodes.TryGetValue(layoutRef.ToGuid(), out ParameterLayoutNode? layout)) {
                continue;
            }
            Guid parameterGuid = layout.ParameterGuid.ToGuid();
            if (!_collectedBank.ParameterNodes.TryGetValue(parameterGuid, out ParameterNode? parameter)) {
                continue;
            }

            bool inRange = minX >= parameter.Minimum - 0.0001f && maxX <= parameter.Maximum + 0.0001f;
            float overflow = 0;
            if (minX < parameter.Minimum) { overflow += parameter.Minimum - minX; }
            if (maxX > parameter.Maximum) { overflow += maxX - parameter.Maximum; }
            float span = Math.Max(parameter.Maximum - parameter.Minimum, 0.0001f);
            float score = inRange ? span : span + overflow * 1000f;

            if (score < bestScore) {
                bestScore = score;
                bestParam = parameterGuid;
            }
        }

        if (bestParam != Guid.Empty && bestScore < 1000f) {
            return bestParam;
        }
        return timelineGuid;
    }
}
