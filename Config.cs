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
    public class Config
    {
        public static Contents contents;

        #region Config create
        public static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "test.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        contents = new Contents();
                        var configString = JsonConvert.SerializeObject(contents, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.Message);
            }
        }
        #endregion

        #region Config Read
        public static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "test.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            contents = JsonConvert.DeserializeObject<Contents>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    TShock.Log.ConsoleError("Test config not found, how about a new one?");
                    CreateConfig();
                    return true;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.Message);
            }
            return false;
        }
        #endregion

        #region Config
        public class Contents
        {
            public string testcooldown = "4h";
            public string testcommandid = "2";
        }
        #endregion

        #region Config reload
        private void Reloadcfg(CommandArgs args)
        {
            if (ReadConfig())
            {
                args.Player.SendMessage("Test config reloaded", Color.Goldenrod);
            }
            else
            {
                args.Player.SendErrorMessage("Something went wrong.");
            }
        }
        #endregion
    }
}
