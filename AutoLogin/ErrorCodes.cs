using System;
using System.Collections.Generic;
using System.Text;

namespace AutoLogin
{
    public class ErrorCode
    {
        /// <summary>
        /// Internal: 13001 | Game code: 2002 | Lobby connection error
        /// </summary>
        public static readonly ErrorCodeEntry LobbyConnectionError = new ErrorCodeEntry
        {
            GameCode = 2002,
            InternalCode = 13001,
            LongDescription = "Error 2002:\nThe lobby server gave an error.\nAuto-reconnection possible.\n",
            ShortDescription = "Lobby connection error"

        };
        /// <summary>
        /// Internal: ????? | Game code: 5006 | Session token expired?
        /// </summary>
        public static readonly ErrorCodeEntry SessionTokenExpired = new ErrorCodeEntry
        {
            GameCode = 5006,
            InternalCode = 13001,
            LongDescription = "Error 5006:\nYour session token has expired.\nYou will need to close the game and login again.\n",
            ShortDescription = "Session token expired"

        };
        /// <summary>
        /// Internal: 16000 | Game code: 90002 | Server connection lost
        /// </summary>
        public static readonly ErrorCodeEntry E90002 = new ErrorCodeEntry
        {
            GameCode = 90002,
            InternalCode = 16000,
            LongDescription = "Error 90002:\nYou have been disconnected from the server.\nAuto-reconnection possible.\n",
            ShortDescription = "Server connection lost"

        };
        /// <summary>
        /// Internal: 13100 | Game code: 5003 | Authorization failed
        /// </summary>
        public static readonly ErrorCodeEntry AuthFailed = new ErrorCodeEntry
        {
            GameCode = 5003,
            InternalCode = 13100,
            LongDescription = "Error 5006:\nYour account info has changed since you started the game.\nYou will need to close the game and login again.\n",
            ShortDescription = "Authorization failed"

        };
        /// <summary>
        /// Internal: 13200 | Game code: ???? | Maintenance
        /// </summary>
        public static readonly ErrorCodeEntry Maintenance = new ErrorCodeEntry
        {
            GameCode = 0,
            InternalCode = 13200,
            LongDescription = "Error ????:\nThe game is currently in Maintance.\nPlease close the game and wait for it to end.",
            ShortDescription = "Maintenance"

        };

    }

    public class ErrorCodeEntry
    {
        public ulong InternalCode { get; set; }
        public ulong GameCode { get; set; }
        public string LongDescription { get; set; }
        public string ShortDescription { get; set; }
    }
}
