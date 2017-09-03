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
    [ApiVersion(2, 1)]
    public class ItemLevel : TerrariaPlugin
    {
        #region Info & Other things
        internal static IDbConnection database;
        public DateTime LastCheck = DateTime.UtcNow;
        public string SavePath = TShock.SavePath;
        public override string Name { get { return "Itemlevel db shit"; } }
        public override string Author { get { return "Tygra"; } }
        public override string Description { get { return "I'm so fucking over this"; } }
        public override Version Version { get { return new Version(0, 1); } }
        public TestPlayer[] Playerlist = new TestPlayer[256];

        public ItemLevel(Main game)
            : base(game)
        {
            Order = 1;
        }
        #endregion

        #region Initialize
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(Test, "test"));
            Commands.ChatCommands.Add(new Command(Querytest, "qtest"));
            Commands.ChatCommands.Add(new Command(Itemlevel, "il", "itemlevel"));
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            if (!Config.ReadConfig())
            {
                TShock.Log.ConsoleError("Config loading failed. Consider deleting it.");
            }
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }
        #endregion

        #region OnInitialize
        private void OnInitialize(EventArgs args)
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    database = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
                        host[0],
                        host.Length == 1 ? "3306" : host[1],
                        TShock.Config.MySqlDbName,
                        TShock.Config.MySqlUsername,
                        TShock.Config.MySqlPassword)
                    };
                    break;

                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, "test.sqlite");
                    database = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;
            }

            SqlTableCreator sqlcreator = new SqlTableCreator(database, database.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());            
            sqlcreator.EnsureTableStructure(new SqlTable("itemlevel",
                new SqlColumn("ID", MySqlDbType.Int32) { Unique = true, Primary = true, AutoIncrement = true },
                new SqlColumn("Itemname", MySqlDbType.Text) { Length = 30 },
                new SqlColumn("Restriction", MySqlDbType.Text)
                ));
            sqlcreator.EnsureTableStructure(new SqlTable("misc",
                new SqlColumn("ID", MySqlDbType.Int32) { Unique = true, Primary = true, AutoIncrement = true },
                new SqlColumn("User", MySqlDbType.Text) { Length = 30 },
                new SqlColumn("CommandID", MySqlDbType.Text),
                new SqlColumn("Date", MySqlDbType.Int32),
                new SqlColumn("Expiration", MySqlDbType.Int32)
                ));
        }
        #endregion

        #region Playerlist Join/Leave
        public void OnJoin(JoinEventArgs args)
        {
            Playerlist[args.Who] = new TestPlayer(args.Who);
        }

        public void OnLeave(LeaveEventArgs args)
        {
            Playerlist[args.Who] = null;
        }
        #endregion

        #region TimeStamp
        public static int UnixTimestamp()
        {
            int unixtime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return unixtime;
        }
        #endregion

        #region TimeSpan
        public static TimeSpan ParseTimeSpan(string s)
        {
            const string Quantity = "quantity";
            const string Unit = "unit";
            const string Days = @"(d(ays?)?)";
            const string Hours = @"(h((ours?)|(rs?))?)";
            const string Minutes = @"(m((inutes?)|(ins?))?)";
            const string Seconds = @"(s((econds?)|(ecs?))?)";

            Regex timeSpanRegex = new Regex(string.Format(@"\s*(?<{0}>\d+)\s*(?<{1}>({2}|{3}|{4}|{5}|\Z))", Quantity, Unit, Days, Hours, Minutes, Seconds), RegexOptions.IgnoreCase);
            MatchCollection matches = timeSpanRegex.Matches(s);
            int l;
            TimeSpan ts = new TimeSpan();
            if (!Int32.TryParse(s.Substring(0, 1), out l))
            {
                return ts;
            }
            foreach (Match match in matches)
            {
                if (Regex.IsMatch(match.Groups[Unit].Value, @"\A" + Days))
                {
                    ts = ts.Add(TimeSpan.FromDays(double.Parse(match.Groups[Quantity].Value)));
                }
                else if (Regex.IsMatch(match.Groups[Unit].Value, Hours))
                {
                    ts = ts.Add(TimeSpan.FromHours(double.Parse(match.Groups[Quantity].Value)));
                }
                else if (Regex.IsMatch(match.Groups[Unit].Value, Minutes))
                {
                    ts = ts.Add(TimeSpan.FromMinutes(double.Parse(match.Groups[Quantity].Value)));
                }
                else if (Regex.IsMatch(match.Groups[Unit].Value, Seconds))
                {
                    ts = ts.Add(TimeSpan.FromSeconds(double.Parse(match.Groups[Quantity].Value)));
                }
                else
                {
                    ts = ts.Add(TimeSpan.FromMinutes(double.Parse(match.Groups[Quantity].Value)));
                }
            }
            return ts;
        }
        #endregion

        #region Querytest
        public void Querytest(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendInfoMessage("0 param");
                return;
            }
            string Switch = args.Parameters[0].ToLower();
            if (Switch == "cooldown")
            {
                int now = UnixTimestamp();
                if (TShock.Config.StorageType.ToLower() == "sqlite")
                {
                    try
                    {
                        using (var reader = database.QueryReader("SELECT * FROM misc WHERE Expiration<@0 AND User=@1", now, args.Player.Name.ToLower()))
                        {
                            if (reader.Read())
                            {
                                reader.Get<string>("Expiration");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TShock.Log.ConsoleError("SQL error" + ex);
                    }
                }
            }
        }
        #endregion

        #region Test
        //működik, beilleszt meg minden. asszem:D
        private void Test(CommandArgs args)
        {
            TimeSpan time = ItemLevel.ParseTimeSpan(Config.contents.testcooldown);
            int date = ItemLevel.UnixTimestamp();
            int expiration = date + (int)time.TotalSeconds;
            string username = args.Player.Name;
            string commandid = Config.contents.testcommandid;
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                database.Query("INSERT INTO misc(User, CommandID, Date, Expiration) VALUES(@0, @1, @2, @3);", username, commandid, date, expiration);
            }
            else
            {
                args.Player.SendErrorMessage("Minek mysql teszthez?");
                return;
            }
        }
        #endregion

        #region Itemlevel
        public void Itemlevel(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendInfoMessage("You can check the required levels for restricted items here.");
                args.Player.SendInfoMessage("If the item name is two or more words, use doubleqoutes areound the name.");
                args.Player.SendInfoMessage("Example: /itemlevel find \"Solar Eruption\"");
                return;
            }
            if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "add")
            {
                if (args.Parameters.Count == 3)
                {
                    string itemname = string.Join(" ", args.Parameters[1]);
                    string restriction = string.Join(" ", args.Parameters[2]);
                    additemlevel(itemname, restriction);
                }
                else
                {
                    args.Player.SendErrorMessage("Invalid syntax.");
                    return;
                }
            }
            if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "del")
            {
                if (args.Parameters.Count > 1)
                {
                    switch (args.Parameters[1].ToLower())
                    {
                        case "id":
                            {

                            }
                            break;

                        case "name":
                            {

                            }
                            break;
                    }
                }
                args.Player.SendErrorMessage("del info");
                return;
            }
            if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "find")
            {

            }
        }
        #endregion

        #region Database things
        private void additemlevel(string itemname, string restriction)
        {
            database.Query("INSERT INTO itemlevel(Itemname, Restriction) VALUES(@0, @1);", itemname, restriction);
        }
        private void delitemlevelbyid(string id)
        {
            database.Query("DELETE FROM itemlevel WHERE ID=@0;", id);
        }
        private void delitemlevelbyname(string itemname)
        {
            database.Query("DELETE FROM itemlevel WHERE Itemname=@0;", itemname);
        }
        #endregion

        #region Config reload
        private void Reloadcfg(CommandArgs args)
        {
            if (Config.ReadConfig())
            {
                args.Player.SendMessage("GeldarV2 Config reloaded.", Color.Goldenrod);
            }
            else
            {
                args.Player.SendErrorMessage("Something went wrong.");
            }
        }
        #endregion
    }
}
