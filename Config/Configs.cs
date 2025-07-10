using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using System.Reflection;
using System.Text;

namespace Revive_Players.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : Attribute
    {
        public int Min { get; }
        public int Max { get; }
        public int Default { get; }
        public string Message { get; }

        public RangeAttribute(int min, int max, int defaultValue, string message)
        {
            Min = min;
            Max = max;
            Default = defaultValue;
            Message = message;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CommentAttribute : Attribute
    {
        public string Comment { get; }

        public CommentAttribute(string comment)
        {
            Comment = comment;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class BreakLineAttribute : Attribute
    {
        public string BreakLine { get; }

        public BreakLineAttribute(string breakLine)
        {
            BreakLine = breakLine;
        }
    }
    public static class Configs
    {
        private static readonly string ConfigDirectoryName = "config";
        private static readonly string ConfigFileName = "config.json";
        private static readonly string PrecacheResources = "ServerPrecacheResources.txt";
        private static string? _configFilePath;
        private static string? _PrecacheResources;
        private static ConfigData? _configData;

        private static readonly JsonSerializerOptions SerializationOptions = new()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            },
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static bool IsLoaded()
        {
            return _configData is not null;
        }

        public static ConfigData GetConfigData()
        {
            if (_configData is null)
            {
                throw new Exception("Config not yet loaded.");
            }

            return _configData;
        }

        public static ConfigData Load(string modulePath)
        {
            var configFileDirectory = Path.Combine(modulePath, ConfigDirectoryName);
            if(!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            _PrecacheResources = Path.Combine(configFileDirectory, PrecacheResources);
            Helper.CreateResource(_PrecacheResources);
            
            _configFilePath = Path.Combine(configFileDirectory, ConfigFileName);
            var defaultConfig = new ConfigData();
            if (File.Exists(_configFilePath))
            {
                try
                {
                    _configData = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(_configFilePath), SerializationOptions);
                }
                catch (JsonException)
                {
                    _configData = MergeConfigWithDefaults(_configFilePath, defaultConfig);
                }
                
                _configData!.Validate();
            }
            else
            {
                _configData = defaultConfig;
                _configData.Validate();
            }

            SaveConfigData(_configData);
            return _configData;
        }

        private static ConfigData MergeConfigWithDefaults(string path, ConfigData defaults)
        {
            var mergedConfig = new ConfigData();
            var jsonText = File.ReadAllText(path);
            
            var readerOptions = new JsonReaderOptions 
            { 
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip 
            };

            using var doc = JsonDocument.Parse(jsonText, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            
            foreach (var jsonProp in doc.RootElement.EnumerateObject())
            {
                var propInfo = typeof(ConfigData).GetProperty(jsonProp.Name);
                if (propInfo == null) continue;

                try
                {
                    var jsonValue = JsonSerializer.Deserialize(
                        jsonProp.Value.GetRawText(), 
                        propInfo.PropertyType,
                        new JsonSerializerOptions
                        {
                            Converters = { new JsonStringEnumConverter() },
                            ReadCommentHandling = JsonCommentHandling.Skip
                        }
                    );
                    propInfo.SetValue(mergedConfig, jsonValue);
                }
                catch (JsonException)
                {
                    propInfo.SetValue(mergedConfig, propInfo.GetValue(defaults));
                }
            }
            
            return mergedConfig;
        }

        private static void SaveConfigData(ConfigData configData)
        {
            if (_configFilePath is null)
                throw new Exception("Config not yet loaded.");

            var json = JsonSerializer.Serialize(configData, SerializationOptions);
            
            var lines = json.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var newLines = new List<string>();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^\s*""(\w+)""\s*:.*");
                bool isPropertyLine = false;
                PropertyInfo? propInfo = null;

                if (match.Success)
                {
                    string propName = match.Groups[1].Value;
                    propInfo = typeof(ConfigData).GetProperty(propName);

                    var breakLineAttr = propInfo?.GetCustomAttribute<BreakLineAttribute>();
                    if (breakLineAttr != null)
                    {
                        string breakLine = breakLineAttr.BreakLine;

                        if (breakLine.Contains("{space}"))
                        {
                            breakLine = breakLine.Replace("{space}", "").Trim();

                            if (breakLineAttr.BreakLine.StartsWith("{space}"))
                            {
                                newLines.Add("");
                            }

                            newLines.Add("// " + breakLine);
                            newLines.Add("");
                        }
                        else
                        {
                            newLines.Add("// " + breakLine);
                        }
                    }

                    var commentAttr = propInfo?.GetCustomAttribute<CommentAttribute>();
                    if (commentAttr != null)
                    {
                        var commentLines = commentAttr.Comment.Split('\n');
                        foreach (var commentLine in commentLines)
                        {
                            newLines.Add("// " + commentLine.Trim());
                        }
                    }

                    isPropertyLine = true;
                }

                newLines.Add(line);

                if (isPropertyLine && propInfo?.GetCustomAttribute<CommentAttribute>() != null)
                {
                    newLines.Add("");
                }
            }

            var adjustedLines = new List<string>();
            foreach (var line in newLines)
            {
                adjustedLines.Add(line);
                if (Regex.IsMatch(line, @"^\s*\],?\s*$"))
                {
                    adjustedLines.Add("");
                }
            }

            File.WriteAllText(_configFilePath, string.Join(Environment.NewLine, adjustedLines), Encoding.UTF8);
        }

        public class ConfigData
        {
            private string? _Version;
            private string? _Link;
            [BreakLine("----------------------------[ ↓ Plugin Info ↓ ]----------------------------{space}")]
            public string Version
            {
                get => _Version!;
                set
                {
                    _Version = value;
                    if (_Version != MainPlugin.Instance.ModuleVersion)
                    {
                        Version = MainPlugin.Instance.ModuleVersion;
                    }
                }
            }

            public string Link
            {
                get => _Link!;
                set
                {
                    _Link = value;
                    if (_Link != "https://github.com/oqyh/cs2-Revive-Players-GoldKingZ")
                    {
                        Link = "https://github.com/oqyh/cs2-Revive-Players-GoldKingZ";
                    }
                }
            }

            [BreakLine("{space}----------------------------[ ↓ Main Config ↓ ]----------------------------{space}")]
            [Comment("Flags Or Group Or SteamID To Allow Revive\nExample:\n\"SteamID: 76561198206086993,76561198974936845 | Flag: @css/vips,@css/admins | Group: #css/vips,#css/admins\"\n\"\" = To Allow Everyone")]
            public string Revive_Flags { get; set; }

            [Comment("Revive Limit\n0 = Unlimited")]
            public int Revive_Limit{ get; set; }
            
            [Comment("Limit By?:\n1 = Team\n2 = Per Player")]
            [Range(1, 2, 1, "[Revive Players] Revive_Limit_Mode: is invalid, setting to default value (1) Please Choose From 1 To 2.\n[Revive Players] 1 = Team\n[Revive Players] 2 = Per Player")]
            public int Revive_Limit_Mode{ get; set; }
            
            [Comment("Reset Revive Limit On Every Round Start?\ntrue = Yes (Reset On Every Round Start)\nfalse = No (Reset On Every New Map)")]
            public bool Revive_Limit_Reset{ get; set; }

            [Comment("How Much Cost To Able To Revive\n0 = Free")]
            public int Revive_Cost { get; set; }

            [Comment("Required [Revive_Cost > 0]\nChoose Mode Of Cost:\n1 = By Money\n2 = By Health")]
            [Range(1, 2, 1, "[Revive Players] Revive_Cost_Mode: is invalid, setting to default value (1) Please Choose From 1 To 2.\n[Revive Players] 1 = By Money\n[Revive Players] 2 = By Health")]
            public int Revive_Cost_Mode { get; set; }

            [Comment("Give CoolDown After Revive (In Secs)\n0 = Disable CoolDown")]
            public float Revive_CoolDown_After_Revive { get; set; }

            [Comment("Required [Revive_CoolDown_After_Revive > 0]\nImmunity From Cooldown Flags Or Group Or SteamID To Allow Revive\nExample:\n\"SteamID: 76561198206086993,76561198974936845 | Flag: @css/vips,@css/admins | Group: #css/vips,#css/admins\"\n\"\" = Disable CoolDown")]
            public string Revive_Immunity_From_Cooldown_Flags { get; set; }

            [Comment("How Close To Revive Someone")]
            public int Revive_Distance { get; set; }
            
            [Comment("How Long (In Secs) Must The Player Hold +USE To Able To Revive")]
            public int Revive_Duration{ get; set; }

            [Comment("After Revive Player What Health Does The Player Revive On")]
            public int Revive_Health{ get; set; }

            [Comment("Cancel Reviving If Reviver Press Anything With +USE?\ntrue = Yes\nfalse = No")]
            public bool Revive_CancelRevivingOnAdditionalInput{ get; set; }

            [Comment("Block Damage On Reviving?\ntrue = Yes\nfalse = No")]
            public bool Revive_BlockDamageOnReviving{ get; set; }

            [Comment("Allow Reviving Only For Team:\n0 = Any Team\n1 = CT Only\n2 = T Only")]
            [Range(0, 2, 0, "[Revive Players] Revive_AllowRevivingTeam: is invalid, setting to default value (0) Please Choose From 0 To 2.\n[Revive Players] 0 = Any Team\n[Revive Players] 1 = CT Only\n[Revive Players] 2 = T Only")]
            public int Revive_AllowRevivingTeam { get; set; }

            [Comment("Path Of Model Animation\n\"\" = Disable Animation")]
            public string Revive_ModelAnimation{ get; set; }

            [Comment("Name Of Animation\n\"\" = Disable Animation")]
            public string Revive_NameAnimation{ get; set; }

            [Comment("Freeze Player On Reviving?\ntrue = Yes\nfalse = No")]
            public bool Revive_FreezeOnReviving{ get; set; }

            [Comment("Before UnFreeze Check Player If Was Freeze Dont UnFreeze Him?\ntrue = Yes\nfalse = No")]
            public bool Revive_DontUnFreezeIfPlayerWasFreezed{ get; set; }

            [BreakLine("{space}----------------------------[ ↓ DeadBody Indicator Config ↓ ]----------------------------{space}")]
            [Comment("Show Arrow On Dead Body?\n0 = No\n1 = Yes + No Animation\n2 = Yes + Animation (Warning Performance)")]
            [Range(0, 2, 1, "[Revive Players] DeadBody_Arrow: is invalid, setting to default value (1) Please Choose From 0 To 2.\n[Revive Players] 0 = No\n[Revive Players] 1 = Yes + No Animation\n[Revive Players] 2 = Yes + Animation (Warning Performance)")]
            public int DeadBody_Arrow{ get; set; }

            [Comment("Required [DeadBody_Arrow > 0]\nArrow Color Red,Green,Blue,Alpha = Can Be 1-255 or 0.01 to 1.0 (R , G , B, Optional A)\nUse This Site (https://rgbacolorpicker.com/) For Color Pick")]
            public string DeadBody_Arrow_Color{ get; set; }

            [Comment("Required [DeadBody_Arrow > 0]\nMove Arrow From The Ground By")]
            public float DeadBody_MoveArrow_From_Ground_By { get; set; }

            [Comment("Show Circle Radius On Dead Body?\ntrue = Yes\nfalse = No")]
            public bool DeadBody_CircleRadius{ get; set; }

            [Comment("Required [DeadBody_CircleRadius = true]\nCircle Radius Color Red,Green,Blue,Alpha = Can Be 1-255 or 0.01 to 1.0 (R , G , B, Optional A)\nUse This Site (https://rgbacolorpicker.com/) For Color Pick")]
            public string DeadBody_CircleRadius_Color{ get; set; }

            [Comment("Required [DeadBody_CircleRadius = true]\nMove Circle Radius From The Ground By")]
            public float DeadBody_MoveCircleRadius_From_Ground_By { get; set; }

            [Comment("Show Text Player Name On Dead Body?\ntrue = Yes\nfalse = No")]
            public bool DeadBody_PlayerNameText{ get; set; }

            [Comment("Required [DeadBody_PlayerNameText = true]\nFont Size")]
            public float DeadBody_PlayerNameText_FontSize{ get; set; }

            [Comment("Required [DeadBody_PlayerNameText = true]\nWorldUnitsPerPx\n0 = Auto (Keep In Mind Some Of FontNames Will Not Work With It)")]
            public float DeadBody_PlayerNameText_WorldUnitsPerPx{ get; set; }

            [Comment("Required [DeadBody_PlayerNameText = true]\nFont Name")]
            public string DeadBody_PlayerNameText_FontName{ get; set; }

            [Comment("Required [DeadBody_PlayerNameText = true]\nFont Color Red,Green,Blue,Alpha = Can Be 1-255 or 0.01 to 1.0 (R , G , B, Optional A)\nUse This Site (https://rgbacolorpicker.com/) For Color Pick")]
            public string DeadBody_PlayerNameText_FontColor{ get; set; }

            [Comment("Required [DeadBody_PlayerNameText = true]\nMove Player Name Text From The Ground By")]
            public float DeadBody_MovePlayerNameText_From_Ground_By { get; set; }

            [BreakLine("{space}----------------------------[ ↓ Utilities  ↓ ]----------------------------{space}")]

            [Comment("Enable Debug Plugin In Server Console (Helps You To Debug Issues You Facing)?\ntrue = Yes\nfalse = No")]
            public bool EnableDebug { get; set; }

            public ConfigData()
            {
                Version = MainPlugin.Instance.ModuleVersion;
                Link = "https://github.com/oqyh/cs2-Revive-Players-GoldKingZ";

                Revive_Flags = "";
                Revive_Limit  = 3;
                Revive_Limit_Mode  = 1;
                Revive_Limit_Reset  = true;
                Revive_Cost = 10;
                Revive_Cost_Mode = 1;
                Revive_CoolDown_After_Revive = 15.0f;
                Revive_Immunity_From_Cooldown_Flags = "SteamID: 76561198206086993,76561198974936845 | Flag: @css/vips,@css/admins | Group: #css/vips,#css/admins";
                Revive_Distance = 40;
                Revive_Duration = 5;
                Revive_Health = 70;
                Revive_CancelRevivingOnAdditionalInput = false;
                Revive_BlockDamageOnReviving = false;
                Revive_AllowRevivingTeam = 0;
                Revive_ModelAnimation = "characters/models/ctm_diver/ctm_diver_varianta.vmdl";
                Revive_NameAnimation = "sh_c4_stand_planting";
                Revive_FreezeOnReviving = true;
                Revive_DontUnFreezeIfPlayerWasFreezed = true;

                DeadBody_Arrow = 1;
                DeadBody_Arrow_Color = "120, 245, 27, 0.45";
                DeadBody_MoveArrow_From_Ground_By = 90.0f;
                DeadBody_CircleRadius = true;
                DeadBody_CircleRadius_Color = "120, 245, 27, 0.45";
                DeadBody_MoveCircleRadius_From_Ground_By = 10.0f;
                DeadBody_PlayerNameText = true;
                DeadBody_PlayerNameText_FontSize = 198.0f;
                DeadBody_PlayerNameText_WorldUnitsPerPx = 0.0f;
                DeadBody_PlayerNameText_FontName = "Tahoma Bold";
                DeadBody_PlayerNameText_FontColor = "120, 245, 27, 0.45";
                DeadBody_MovePlayerNameText_From_Ground_By = 40.0f;

                EnableDebug = false;
            }
            public void Validate()
            {
                foreach (var prop in GetType().GetProperties())
                {
                    var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
                    if (rangeAttr != null && prop.PropertyType == typeof(int))
                    {
                        int value = (int)prop.GetValue(this)!;
                        if (value < rangeAttr.Min || value > rangeAttr.Max)
                        {
                            prop.SetValue(this, rangeAttr.Default);
                            Helper.DebugMessage(rangeAttr.Message,false);
                        }
                    }
                }
            }
        }
    }
}
