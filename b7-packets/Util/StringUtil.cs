using System;
using System.Text;

namespace b7.Packets.Util
{
    public class StringUtil
    {
        public static string Unescape(string s, Func<char, string> unescaper)
        {
            var sb = new StringBuilder();

            bool isEscaping = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (isEscaping)
                {
                    string result = unescaper(c);
                    if (result != null)
                        sb.Append(result);
                    isEscaping = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        isEscaping = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }

    }
}
