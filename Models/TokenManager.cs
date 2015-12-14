using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class TokenManager
    {
        private static TokenManager _instance = null;

        public static TokenManager GetInstance()
        {
            if (_instance == null)
                _instance = new TokenManager();

            return _instance;
        }

        private TokenManager()
        {
            // TODO: AppSettingsからタイムアウト時間を取得
            var value = ConfigurationManager.AppSettings["ListenerTokenTimeout"];
            if (value == null || !long.TryParse(value, out this._expiredDuration))
            {
                this._expiredDuration = 600;
            }
        }

        private readonly long _expiredDuration;

        private readonly Dictionary<string, Tuple<string, DateTime>> _tokens = new Dictionary<string, Tuple<string, DateTime>>();

        public string GetTokenInfo(string token)
        {
            lock (_tokens)
            {
                if (token == null || !_tokens.ContainsKey(token))
                    return null;

                var now = DateTime.Now;
                var item = _tokens[token];

                var diff = item.Item2 - now;
                if (diff.TotalSeconds < _expiredDuration)
                {
                    // OK
                    return item.Item1;
                }
                else
                {
                    // TODO: 定期的に削除
                    return null;
                }
            }
        }

        public void RemoveToken(string token)
        {
            lock (_tokens)
            {
                _tokens.Remove(token);
            }
        }

        public string CreateToken(string roomId)
        {
            if (roomId == null)
                return null;

            var id = Guid.NewGuid().ToString();
            lock (_tokens)
            {
                _tokens.Add(id, new Tuple<string, DateTime>(roomId, DateTime.Now));
            }
            return id;
        }
    }
}
