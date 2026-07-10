using System.Xml.Linq;
using FModBankParser.Enums;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Transitions;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler {
    private void AppendTimelineMarkersAndTransitions(
        List<object> documentObjects,
        EventNode eventNode,
        Guid timelineGuid,
        List<object> timelineContent,
        List<Guid> markerTrackGuids
    ) {
        if (!_collectedBank.TimelineNodes.TryGetValue(timelineGuid, out TimelineNode? timeline)) {
            return;
        }

        HashSet<Guid> namedMarkerIds = timeline.TimelineNamedMarkers.Select(m => m.BaseGuid.ToGuid()).ToHashSet();
        List<Guid> timelineMarkerGuids = [];

        foreach (FTimelineNamedMarker marker in timeline.TimelineNamedMarkers) {
            Guid markerGuid = marker.BaseGuid.ToGuid();
            Guid markerTrackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{markerGuid}/MarkerTrack");
            markerTrackGuids.Add(markerTrackGuid);
            timelineMarkerGuids.Add(markerGuid);

            documentObjects.Add(XmlBuilder.Object("MarkerTrack", markerTrackGuid));

            List<object> markerContent = [
                XmlBuilder.Property("position", marker.Position / TimelineUnitsPerSecond),
            ];
            if (!string.IsNullOrEmpty(marker.Name)) {
                markerContent.Add(XmlBuilder.Property("name", marker.Name));
            }
            markerContent.Add(XmlBuilder.Property("length", marker.Length / TimelineUnitsPerSecond));
            markerContent.Add(XmlBuilder.Relationship("timeline", timelineGuid));
            markerContent.Add(XmlBuilder.Relationship("markerTrack", markerTrackGuid));
            documentObjects.Add(XmlBuilder.Object("NamedMarker", markerGuid, markerContent.ToArray()));
        }

        foreach (FTimelineTempoMarker tempo in timeline.TimelineTempoMarkers) {
            Guid markerGuid = tempo.BaseGuid.ToGuid();
            Guid markerTrackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{markerGuid}/MarkerTrack");
            markerTrackGuids.Add(markerTrackGuid);
            timelineMarkerGuids.Add(markerGuid);

            documentObjects.Add(XmlBuilder.Object("MarkerTrack", markerTrackGuid));
            documentObjects.Add(XmlBuilder.Object("TempoMarker", markerGuid,
                XmlBuilder.Property("position", tempo.Position / TimelineUnitsPerSecond),
                XmlBuilder.Property("tempo", tempo.Tempo),
                XmlBuilder.Relationship("timeline", timelineGuid),
                XmlBuilder.Relationship("markerTrack", markerTrackGuid)
            ));
        }

        foreach ((Guid transitionGuid, BaseTransitionNode transition) in _collectedBank.TransitionNodes) {
            if (transition is not TransitionRegionNode region) { continue; }

            Guid destinationGuid = region.DestinationGuid.ToGuid();
            bool belongsToTimeline = namedMarkerIds.Contains(destinationGuid) || namedMarkerIds.Contains(transitionGuid);
            if (!belongsToTimeline) { continue; }

            // Destination-only / zero-width self markers are already emitted as NamedMarker.
            if (destinationGuid == transitionGuid && region.Start == region.End) { continue; }

            Guid markerTrackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{transitionGuid}/MarkerTrack");
            markerTrackGuids.Add(markerTrackGuid);
            timelineMarkerGuids.Add(transitionGuid);
            documentObjects.Add(XmlBuilder.Object("MarkerTrack", markerTrackGuid));

            double position = region.Start / TimelineUnitsPerSecond;
            double length = Math.Max(0, (region.End - region.Start) / TimelineUnitsPerSecond);

            List<object> regionContent = [
                XmlBuilder.Property("position", position),
                XmlBuilder.Property("length", length),
                XmlBuilder.Property("probability", region.TransitionChancePercent),
            ];

            List<Guid> conditionGuids = [];
            AppendTransitionConditions(documentObjects, transitionGuid, region, conditionGuids);
            if (conditionGuids.Count > 0) {
                regionContent.Add(XmlBuilder.Relationship("triggerConditions", conditionGuids));
            }

            if (transition.TransitionBody is not null) {
                Guid transitionTimelineGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{transitionGuid}/TransitionTimeline");
                regionContent.Add(XmlBuilder.Relationship("transitionTimeline", transitionTimelineGuid));
                AppendTransitionTimeline(documentObjects, transitionGuid, transitionTimelineGuid, transition.TransitionBody, eventNode);
            }

            if (destinationGuid != Guid.Empty) {
                regionContent.Add(XmlBuilder.Relationship("destination", destinationGuid));
            }
            regionContent.Add(XmlBuilder.Relationship("timeline", timelineGuid));
            regionContent.Add(XmlBuilder.Relationship("markerTrack", markerTrackGuid));

            documentObjects.Add(XmlBuilder.Object("TransitionRegion", transitionGuid, regionContent.ToArray()));
        }

        if (timelineMarkerGuids.Count > 0) {
            timelineContent.Add(XmlBuilder.Relationship("markers", timelineMarkerGuids));
        }
    }

    private void AppendTransitionConditions(
        List<object> documentObjects,
        Guid transitionGuid,
        TransitionRegionNode region,
        List<Guid> conditionGuids
    ) {
        if (region.LegacyParameterConditions is { } legacy && !legacy.BaseGuid.IsEmpty) {
            Guid conditionGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{transitionGuid}/ParameterCondition");
            conditionGuids.Add(conditionGuid);
            documentObjects.Add(XmlBuilder.Object("ParameterCondition", conditionGuid,
                XmlBuilder.Property("minimum", legacy.Minimum),
                XmlBuilder.Property("maximum", legacy.Maximum),
                XmlBuilder.Relationship("parameter", legacy.BaseGuid.ToGuid())
            ));
            return;
        }

        FModGuid? parameterGuid = null;
        float? minimum = null;
        float? maximum = null;
        foreach (FEvaluator evaluator in region.Evaluators) {
            if (evaluator.Type == EEvaluatorType.Type11 && evaluator.Data is FModGuid guid) {
                parameterGuid = guid;
            }
            else if (evaluator.Type == EEvaluatorType.Type20 && evaluator.Data is float[] range && range.Length >= 2) {
                minimum = range[0];
                maximum = range[1];
            }
        }

        if (parameterGuid is null || minimum is null || maximum is null) { return; }

        Guid evalConditionGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{transitionGuid}/ParameterCondition");
        conditionGuids.Add(evalConditionGuid);
        documentObjects.Add(XmlBuilder.Object("ParameterCondition", evalConditionGuid,
            XmlBuilder.Property("minimum", minimum.Value),
            XmlBuilder.Property("maximum", maximum.Value),
            XmlBuilder.Relationship("parameter", parameterGuid.Value.ToGuid())
        ));
    }

    private void AppendTransitionTimeline(
        List<object> documentObjects,
        Guid transitionGuid,
        Guid transitionTimelineGuid,
        TransitionTimelineNode body,
        EventNode eventNode
    ) {
        List<Guid> moduleGuids = [];
        double lengthSeconds = body.Length / TimelineUnitsPerSecond;
        double destStart = body.LeadInLength > 0 ? body.LeadInLength / TimelineUnitsPerSecond : lengthSeconds;

        List<Guid> groupBusGuids = ResolveEventGroupBuses(eventNode, eventNode.MasterTrackGuid.ToGuid());
        List<Guid> stripGuids = [.. groupBusGuids];
        if (!eventNode.MasterTrackGuid.IsEmpty) {
            stripGuids.Add(eventNode.MasterTrackGuid.ToGuid());
        }

        foreach (Guid stripGuid in stripGuids) {
            Guid sourceGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{transitionGuid}/{stripGuid}/TransitionSource");
            Guid destGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{transitionGuid}/{stripGuid}/TransitionDestination");
            moduleGuids.Add(sourceGuid);
            moduleGuids.Add(destGuid);

            documentObjects.Add(XmlBuilder.Object("TransitionSourceSound", sourceGuid,
                XmlBuilder.Property("length", 0)
            ));
            documentObjects.Add(XmlBuilder.Object("TransitionDestinationSound", destGuid,
                XmlBuilder.Property("start", destStart),
                XmlBuilder.Property("length", 0)
            ));
        }

        // Prefer real trigger-box instruments when present.
        foreach (FTriggerBox box in body.TimeLockedTriggerBoxes.Concat(body.TriggeredTriggerBoxes)) {
            Guid instrumentGuid = box.Guid.ToGuid();
            if (!moduleGuids.Contains(instrumentGuid)) {
                moduleGuids.Add(instrumentGuid);
            }
        }

        List<object> content = [
            XmlBuilder.Property("length", lengthSeconds),
        ];
        if (moduleGuids.Count > 0) {
            content.Add(XmlBuilder.Relationship("modules", moduleGuids));
        }
        documentObjects.Add(XmlBuilder.Object("TransitionTimeline", transitionTimelineGuid, content.ToArray()));
    }
}
