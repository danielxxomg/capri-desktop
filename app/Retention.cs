using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CapriDesktop
{
    // What she keeps, and for how long. Chat logs and the sights she kept go after a week; the daily chat
    // transcripts go after a year, unless they chose to keep everything. Her notes stay until they delete them.
    internal static class Retention
    {
        internal const int ChatDays = 7;
        internal const int Days = 365;

        // Deletes only whole days older than the cutoff, read from the file's own date, so a copy or a touched
        // file can't keep a year-old day alive. Returns how many days and chats went.
        internal static string Sweep(string transcripts, string chat, bool keepForever, DateTime now)
        {
            if (keepForever) return "Kept every chat and transcript.";
            int days = 0, chats = 0;
            if (transcripts != null && Directory.Exists(transcripts))
            {
                DateTime cutoff = now.Date.AddDays(-Days);
                string[] files;
                try { files = Directory.GetFiles(transcripts, "????-??-??.md"); }
                catch (IOException) { files = new string[0]; }
                catch (UnauthorizedAccessException) { files = new string[0]; }
                foreach (string file in files)
                {
                    DateTime day;
                    if (!DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day) || day >= cutoff) continue;
                    try { File.Delete(file); days++; }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            if (chat != null && File.Exists(chat))
            {
                DateTime at;
                if (ChatAt(chat, out at) && now - at > TimeSpan.FromDays(ChatDays))
                {
                    try { File.Delete(chat); chats++; }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            return "Removed " + days + " old transcripts and " + chats + " old chats.";
        }

        // When they last talked, as saved in chat.json. A file without a readable time is left alone.
        private static bool ChatAt(string path, out DateTime at)
        {
            at = DateTime.MinValue;
            string text;
            try { text = File.ReadAllText(path); }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            Match found = Regex.Match(text, "\"At\"\\s*:\\s*\"([^\"]+)\"");
            if (!found.Success) return false;
            return DateTime.TryParse(found.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at);
        }
    }
}
