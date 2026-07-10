using System.Runtime.CompilerServices;
using System.Text;

namespace BANK2FSPRO;

public class DebugHelper {
    private static readonly Dictionary<(string File, int Line), HashSet<string>> Seen = new();
    private static readonly Dictionary<(string File, int Line, string Thing), int> Combos = new();

    private static readonly int[] Rainbow = [196, 208, 226, 118, 51, 39, 201, 213];

    public static void UniqueLog(string thing, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
        (string file, int line) key = (file, line);
        Seen.TryAdd(key, []);
        if (!Seen[key].Add(thing)) { return; }
        Console.WriteLine(thing);
    }

    public static void ComboLog(string thing, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
        (string file, int line, string thing) key = (file, line, thing);
        Combos.TryGetValue(key, out int count);
        count++;
        Combos[key] = count;

        string reset = "\x1b[0m";
        string? milestone = MilestoneBanner(count);
        if (milestone is not null) { Console.WriteLine(milestone); }

        string styled = StyleMessage(thing, count);
        string badge = ComboBadge(count);
        string flair = SideFlair(count);
        string bar = ComboBar(count);
        Console.WriteLine($"{flair}{styled}  {badge}{reset}  {bar}");
    }

    private static string StyleMessage(string thing, int count) {
        if (count >= 500) return RainbowPulse(thing, bold: true);
        if (count >= 200) return RainbowPulse(thing, bold: true);
        if (count >= 100) return $"{Bold()}{Fg(201)}{thing}";
        if (count >= 75)  return $"{Bold()}{Fg(196)}{Bg(52)}{thing}";
        if (count >= 50)  return $"{Bold()}{Fg(196)}{thing}";
        if (count >= 40)  return $"{Bold()}{Fg(202)}{thing}";
        if (count >= 30)  return $"{Bold()}{Fg(208)}{thing}";
        if (count >= 25)  return $"{Bold()}{Fg(214)}{thing}";
        if (count >= 20)  return $"{Bold()}{Fg(220)}{thing}";
        if (count >= 15)  return $"{Bold()}{Fg(226)}{thing}";
        if (count >= 12)  return $"{Fg(190)}{thing}";
        if (count >= 10)  return $"{Fg(118)}{thing}";
        if (count >= 8)   return $"{Fg(82)}{thing}";
        if (count >= 6)   return $"{Fg(51)}{thing}";
        if (count >= 4)   return $"{Fg(45)}{thing}";
        if (count >= 3)   return $"{Fg(39)}{thing}";
        if (count >= 2)   return $"{Fg(245)}{thing}";
        return $"{Fg(250)}{thing}";
    }

    private static string ComboBadge(int count) => count switch {
        >= 1000 => $"{Bold()}{Fg(201)}*** x{count} TRANSCENDENT ***",
        >= 777  => $"{Bold()}{Fg(213)}*** x{count} JACKPOT ***",
        >= 500  => $"{Bold()}{Fg(201)}*** x{count} ASCENDED ***",
        >= 250  => $"{Bold()}{Fg(196)}### x{count} UNSTOPPABLE ###",
        >= 200  => $"{Bold()}{Fg(196)}### x{count} DEMIGOD ###",
        >= 150  => $"{Bold()}{Fg(197)}### x{count} GODLIKE ###",
        >= 100  => $"{Bold()}{Fg(201)}*** x{count} LEGENDARY ***",
        >= 90   => $"{Bold()}{Fg(199)}** x{count} MYTHIC **",
        >= 75   => $"{Bold()}{Fg(196)}**** x{count} APOCALYPSE ****",
        >= 60   => $"{Bold()}{Fg(196)}*** x{count} NUCLEAR ***",
        >= 50   => $"{Bold()}{Fg(196)}*** x{count} INSANE ***",
        >= 40   => $"{Bold()}{Fg(202)}** x{count} RAMPAGE **",
        >= 35   => $"{Bold()}{Fg(208)}** x{count} BLAZING **",
        >= 30   => $"{Bold()}{Fg(208)}** x{count} INFERNO **",
        >= 25   => $"{Bold()}{Fg(214)}** x{count} ON FIRE **",
        >= 20   => $"{Bold()}{Fg(220)}* x{count} SIZZLING *",
        >= 18   => $"{Bold()}{Fg(226)}* x{count} SPICY *",
        >= 15   => $"{Bold()}{Fg(226)}* x{count} HOT *",
        >= 13   => $"{Fg(190)}x{count} !!!",
        >= 12   => $"{Fg(154)}x{count} !!",
        >= 10   => $"{Fg(118)}x{count} !! COMBO",
        >= 9    => $"{Fg(82)}x{count} !!!",
        >= 8    => $"{Fg(82)}x{count} !!",
        >= 7    => $"{Fg(51)}x{count} !",
        >= 6    => $"{Fg(51)}x{count} ~",
        >= 5    => $"{Fg(45)}x{count} !",
        >= 4    => $"{Fg(39)}x{count}",
        >= 3    => $"{Fg(39)}x{count}",
        >= 2    => $"{Fg(245)}x{count}",
        _       => $"{Fg(250)}x{count}",
    };

    private static string SideFlair(int count) {
        if (count < 3) return "";
        if (count >= 500) return $"{Fg(201)}>>>>>>>> ";
        if (count >= 200) return $"{Fg(201)}>>>>>> ";
        if (count >= 100) return $"{Fg(201)}***** ";
        if (count >= 75)  return $"{Fg(196)}#### ";
        if (count >= 50)  return $"{Fg(196)}### ";
        if (count >= 25)  return $"{Fg(208)}>> ";
        if (count >= 15)  return $"{Fg(226)}* ";
        if (count >= 10)  return $"{Fg(118)}>> ";
        if (count >= 5)   return $"{Fg(51)}> ";
        return $"{Fg(39)}. ";
    }

    private static string ComboBar(int count) {
        int filled = Math.Min(count, 40);
        int tier = count switch {
            >= 100 => 201,
            >= 50  => 196,
            >= 25  => 208,
            >= 15  => 226,
            >= 10  => 118,
            >= 5   => 51,
            _      => 39,
        };

        var sb = new StringBuilder();
        sb.Append(Fg(240)).Append('[');
        for (int i = 0; i < filled; i++) {
            int color = count >= 100 ? Rainbow[i % Rainbow.Length] : tier;
            char ch = count switch {
                >= 100 => (i % 3 == 0 ? '#' : '='),
                >= 50  => '#',
                >= 25  => '=',
                >= 10  => '+',
                _      => '-',
            };
            sb.Append(Fg(color)).Append(ch);
        }
        if (filled < 20) {
            sb.Append(Fg(236)).Append(new string('.', 20 - filled));
        }
        sb.Append(Fg(240)).Append(']');
        sb.Append("\x1b[0m");
        return sb.ToString();
    }

    private static string? MilestoneBanner(int count) {
        string? title = count switch {
            5    => "COMBO START",
            10   => "DOUBLE DIGITS",
            15   => "HEATING UP",
            25   => "ON FIRE",
            50   => "HALFWAY TO LEGEND",
            75   => "APPROACHING APOCALYPSE",
            100  => "*** CENTURY CLUB ***",
            150  => "GOD MODE UNLOCKED",
            200  => "DEMIGOD STATUS",
            250  => "UNSTOPPABLE FORCE",
            500  => "*** ASCENSION ***",
            777  => "*** JACKPOT ***",
            999  => "ONE AWAY FROM GLORY",
            1000 => "*** TRANSCENDENCE ***",
            _ when count > 0 && count % 100 == 0 => $"x{count} MILESTONE",
            _ => null,
        };
        if (title is null) return null;

        string line = new('=', Math.Min(12 + title.Length / 2, 28));
        return $"{RainbowPulse($"  {line} {title} {line}  ", bold: true)}\x1b[0m";
    }

    private static string RainbowPulse(string text, bool bold = false) {
        var sb = new StringBuilder();
        if (bold) sb.Append(Bold());
        int offset = Environment.TickCount / 80;
        for (int i = 0; i < text.Length; i++) {
            sb.Append(Fg(Rainbow[(i + offset) % Rainbow.Length]));
            sb.Append(text[i]);
        }
        return sb.ToString();
    }

    private static string Fg(int c) => $"\x1b[38;5;{c}m";
    private static string Bg(int c) => $"\x1b[48;5;{c}m";
    private static string Bold() => "\x1b[1m";
}
