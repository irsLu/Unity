using UnityEngine;

namespace TFW
{
    public class D
    {
        public static void Assert(bool a)
        {

        }

        public static void Warning(object msg, params object[] para) {
            string content = msg.ToString();

            if (para != null && para.Length > 0)
                content = string.Format(content, para);


            Debug.LogWarning(content);
        }

        public static void Log(object msg, params object[] para) {
            string content = msg.ToString();

            if (para != null && para.Length > 0)
                content = string.Format(content, para);


            Debug.LogWarning(content);
        }

    }
}