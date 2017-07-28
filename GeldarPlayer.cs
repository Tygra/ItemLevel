#region Refs
using System;
using System.Data;
using System.IO;
using System.ComponentModel;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

//Terraria related refs
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Localization;
using Newtonsoft.Json;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
#endregion

namespace ItemLevel
{
    public class GeldarPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public GeldarPlayer(int index)
        {
            this.Index = index;
        }
    }
}
