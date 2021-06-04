using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class DMGMissionLoader
{
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }

    private class Pair<T1, T2>
    {
        public T1 First;
        public T2 Second;
    }

    private delegate void NewBombDelegate();

    private delegate void ValidateBombDelegate();

    private static readonly Regex TokenRegex = new Regex(@"
			\G\s*()(?:  # Group 1 marks the position after whitespace; used for completion
                ////(?<MissionName>.+)|
                ///(?<DescriptionLine>.+)|
                /\*\*\s+?(?<MissionName2>\S[\s\S]+?)[\r\n]+\s+?(?<Description>\S[\s\S]+?)(?:\*/|$)|
				//.*|/\*[\s\S]*?(?:\*/|$)|  # Comment
				(?<Close>\))|
				(?:time:)?(?<Time1>\d{1,9}):(?<Time2>\d{1,9})(?::(?<Time3>\d{1,9}))?(?!\S)|
				(?<Strikes>\d{1,9})X(?!\S)|
				(?<Setting>strikes|needyactivationtime|widgets|nopacing|frontonly|factory)\b(?::(?<Value>(?:,\s*|[^\s)])*))?|
				(?:(?<Count>\d{1,9})\s*[;*]\s*)?
				(?:
                    (?<Open>\()|
					(?<ID>(?:[^\s'"",+)]|(?<q>['""])(?:(?!\k<q>)[\s\S])*(?:\k<q>|(?<Error>))|[,+]\s*)+)  # Module pool; ',' or '+' may be followed by spaces; 'Error' group catches unclosed quotes
				)
			)", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);


    private static readonly Regex ContinuationRegex = new Regex(@"\\[\r\n]+(///?)?\s+");

    private static readonly Regex FileNameRegex = new Regex(@"^(\d+\. ?)?(?<SectionTitle>.+)(\.txt)?$");

    private static readonly string[] FactoryModes =
    {
        "static",
        "finite",
        "finitegtime",
        "finitegstrikes",
        "finitegtimestrikes",
        "infinite",
        "infinitegtime",
        "infinitegstrikes",
        "infinitegtimestrikes"
    };

    private const string MissionFolder = "Missions";

    [MenuItem("Keep Talking ModKit/Missions/Load DMG Mission File", false, 9999)]
    public static void LoadDmgMission()
    {
        // Create the mission folder if it does not exist
        if (!AssetDatabase.IsValidFolder("Assets/" + MissionFolder))
        {
            AssetDatabase.CreateFolder("Assets", MissionFolder);
        }

        // File open dialog
        var path = EditorUtility.OpenFilePanel("Open DMG Mission", "", "txt");
        if (path.Length == 0) return;

        Debug.LogFormat("Loading DMG Mission at '{0}'...", path);

        // Read file data
        var dmgString = File.ReadAllText(path);
        KMMission mission;
        try
        {
            // Create mission
            mission = CreateMissionFromDmgString(dmgString);
        }
        catch (ParseException e)
        {
            Debug.LogErrorFormat("Error while parsing DMG string: {0}", e.Message);
            return;
        }

        Debug.LogFormat("Successfully parsed DMG mission, creating asset...");

        // Determine output path
        var missionPath = "Assets/" + MissionFolder + "/" +
                          (mission.DisplayName.Length == 0
                              ? "mission"
                              : mission.DisplayName) +
                          ".asset";
        var outputPath = missionPath;

        // Check if the mission already exists, ask the user whether they want to overwrite the file or create a new file
        if (AssetImporter.GetAtPath(outputPath) != null && !EditorUtility.DisplayDialog("Confirm Overwrite",
            "The mission that you are trying to load already exists. Would you like to overwrite it?", "Overwrite",
            "Make New Mission"))
        {
            outputPath =
                AssetDatabase.GenerateUniqueAssetPath(missionPath);
        }

        // Create and open the asset
        AssetDatabase.CreateAsset(mission, outputPath);
        AssetImporter.GetAtPath(outputPath).assetBundleName = AssetBundler.BUNDLE_FILENAME;
        EditorGUIUtility.PingObject(mission);
    }

    [MenuItem("Keep Talking ModKit/Missions/Load DMG Mission Folder", false, 10000)]
    public static void LoadDmgPack()
    {
        // Create the mission folder if it does not exist
        if (!AssetDatabase.IsValidFolder("Assets/" + MissionFolder))
        {
            AssetDatabase.CreateFolder("Assets", MissionFolder);
        }

        // Folder open dialog
        var packPath = EditorUtility.OpenFolderPanel("Open DMG Mission Folder", "", "");
        if (packPath.Length == 0) return;

        // Determine the output folder name
        var outputFolderName = Path.GetFileName(packPath);

        // Sort section paths
        var sectionPaths = Directory.GetDirectories(packPath);
        sectionPaths = sectionPaths.OrderPaths().ToArray();

        // Check for asset folder conflicts
        if (AssetDatabase.IsValidFolder("Assets/" + MissionFolder + "/" + outputFolderName))
        {
            if (
                EditorUtility.DisplayDialog("Confirm Rename",
                    "The pack that you are trying to load already exists. Would you like to rename it?", "Rename",
                    "Cancel"))
            {
                outputFolderName =
                    Path.GetFileName(
                        AssetDatabase.GenerateUniqueAssetPath("Assets/" + MissionFolder + "/" + outputFolderName));
            }
            else
            {
                return;
            }
        }

        // Create the pack folder
        if (!AssetDatabase.IsValidFolder("Assets/" + MissionFolder + "/" + outputFolderName))
        {
            AssetDatabase.CreateFolder("Assets/" + MissionFolder, outputFolderName);
        }

        Debug.LogFormat("Loading DMG Mission Pack at '{0}'...", packPath);

        // Load each section
        var sections = new List<KMMissionTableOfContents.Section>();
        foreach (var sectionPath in sectionPaths)
        {
            // Create the section object
            var section = new KMMissionTableOfContents.Section
            {
                Title = FileNameRegex.Matches(Path.GetFileName(sectionPath))[0].Groups["SectionTitle"].Value,
                MissionIDs = new List<string>()
            };

            // Sort all missions
            var missionPaths = Directory.GetFiles(sectionPath);
            missionPaths = missionPaths.OrderPaths().ToArray();

            // Get the section folder name
            var sectionFolderName = (sections.Count + 1) + ". " + section.Title;
            var sectionRoot = "Assets/" + MissionFolder + "/" + outputFolderName +
                              "/" + sectionFolderName;

            // Create the section folder
            if (!AssetDatabase.IsValidFolder(sectionRoot))
            {
                AssetDatabase.CreateFolder("Assets/" + MissionFolder + "/" + outputFolderName, sectionFolderName);
            }

            // Create each mission
            foreach (var missionPath in missionPaths)
            {
                // Read the mission data
                var dmgString = File.ReadAllText(missionPath);
                KMMission mission;
                try
                {
                    // Parse the DMG string
                    mission = CreateMissionFromDmgString(dmgString);
                }
                catch (ParseException e)
                {
                    Debug.LogErrorFormat("Error while parsing DMG string: {0}", e.Message);
                    continue;
                }

                // Create the mission asset
                var outputPath = AssetDatabase.GenerateUniqueAssetPath(sectionRoot + "/" +
                                                                       (mission.DisplayName.Length == 0
                                                                           ? "mission"
                                                                           : mission.DisplayName) +
                                                                       ".asset");
                AssetDatabase.CreateAsset(mission, outputPath);

                // Add the mission to the section
                section.MissionIDs.Add(mission.ID);
            }

            // Add the section to the ToC
            sections.Add(section);
        }

        // Create the ToC
        var toc = ScriptableObject.CreateInstance<KMMissionTableOfContents>();
        toc.name = outputFolderName;
        toc.Sections = sections;
        var tocPath = "Assets/" + MissionFolder + "/" + outputFolderName + "/TOC-" + outputFolderName + ".asset";

        // Create the ToC asset
        AssetDatabase.CreateAsset(toc, tocPath);
        AssetImporter.GetAtPath(tocPath).assetBundleName = AssetBundler.BUNDLE_FILENAME;
        EditorGUIUtility.PingObject(toc);
    }

    public static KMMission CreateMissionFromDmgString(string dmgString)
    {
        string errorMessage = null;

        // Parse the DMG string
        var matches = TokenRegex.Matches(ContinuationRegex.Replace(dmgString, ""));
        if (matches.Count == 0)
        {
            throw new ParseException("invalid DMG string provided");
        }

        // Bomb settings
        KMGeneratorSetting currentBomb = null;
        List<KMGeneratorSetting> bombs = null;

        // Flags
        var bombRepeatCount = 0;
        int? defaultTime = null,
            defaultStrikes = null,
            defaultNeedyActivationTime = null,
            defaultWidgetCount = null;
        var defaultFrontOnly = false;
        bool timeSpecified = false,
            strikesSpecified = false,
            needyActivationTimeSpecified = false,
            widgetCountSpecified = false,
            missionNameSpecified = false;
        int? factoryMode = null;

        // Mission setup
        var mission = ScriptableObject.CreateInstance<KMMission>();
        mission.PacingEventsEnabled = true;
        var pools = new List<KMComponentPool>();
        var missionDescription = "";

        // New bomb handler
        NewBombDelegate newBomb = delegate
        {
            currentBomb = new KMGeneratorSetting {FrontFaceOnly = defaultFrontOnly};
            timeSpecified = strikesSpecified = needyActivationTimeSpecified =
                widgetCountSpecified = false;
            pools = new List<KMComponentPool>();
        };

        // Bomb validation handler
        ValidateBombDelegate validateBomb = delegate
        {
            // Load the pools and setup default generator settings
            currentBomb.ComponentPools = pools;
            if (!timeSpecified) currentBomb.TimeLimit = defaultTime ?? currentBomb.GetComponentCount() * 120;
            if (!strikesSpecified)
                currentBomb.NumStrikes = defaultStrikes ?? Math.Max(3, currentBomb.GetComponentCount() / 12);
            if (!needyActivationTimeSpecified && defaultNeedyActivationTime.HasValue)
                currentBomb.TimeBeforeNeedyActivation = defaultNeedyActivationTime.Value;
            if (!widgetCountSpecified && defaultWidgetCount.HasValue)
                currentBomb.OptionalWidgetCount = defaultWidgetCount.Value;
        };

        // Deal with each token
        foreach (Match match in matches)
        {
            // Inline mission name
            if (match.Groups["MissionName"].Success)
            {
                if (missionNameSpecified)
                {
                    errorMessage = "mission name specified multiple times";
                    goto error;
                }

                missionNameSpecified = true;

                mission.DisplayName = match.Groups["MissionName"].Value.Trim();
            }
            // Inline description
            else if (match.Groups["DescriptionLine"].Success)
            {
                missionDescription += match.Groups["DescriptionLine"].Value.Trim() + "\n";

                mission.Description = missionDescription.Trim();
            }
            // Multiline name + description
            else if (match.Groups["MissionName2"].Success && match.Groups["Description"].Success)
            {
                if (missionNameSpecified)
                {
                    errorMessage = "mission name specified multiple times";
                    goto error;
                }

                missionNameSpecified = true;

                mission.DisplayName = match.Groups["MissionName2"].Value.Trim();
                mission.Description =
                    match.Groups["Description"].Value.Split('\n').Select(line => line.Trim()).Join("\n");
            }
            // Timer
            else if (match.Groups["Time1"].Success)
            {
                if (timeSpecified)
                {
                    errorMessage = "time specified multiple times";
                    goto error;
                }

                timeSpecified = true;

                // Parse time
                var time = match.Groups["Time3"].Success
                    ? int.Parse(match.Groups["Time1"].Value) * 3600 + int.Parse(match.Groups["Time2"].Value) * 60 +
                      int.Parse(match.Groups["Time3"].Value)
                    : int.Parse(match.Groups["Time1"].Value) * 60 + int.Parse(match.Groups["Time2"].Value);
                if (time <= 0)
                {
                    errorMessage = "invalid time limit";
                    goto error;
                }

                if (currentBomb != null) currentBomb.TimeLimit = time;
                else defaultTime = time;
            }
            // Strike count
            else if (match.Groups["Strikes"].Success || match.Groups["Setting"].Value
                .Equals("strikes", StringComparison.InvariantCultureIgnoreCase))
            {
                if (strikesSpecified)
                {
                    errorMessage = ("strikes specified multiple times");
                    goto error;
                }

                strikesSpecified = true;

                var strikes = int.Parse(match.Groups["Strikes"].Success
                    ? match.Groups["Strikes"].Value
                    : match.Groups["Value"].Value);
                if (strikes <= 0)
                {
                    errorMessage = "invalid strike limit";
                    goto error;
                }

                if (currentBomb != null) currentBomb.NumStrikes = strikes;
                else defaultStrikes = strikes;
            }
            // Various settings
            else if (match.Groups["Setting"].Success)
            {
                switch (match.Groups["Setting"].Value.ToLowerInvariant())
                {
                    // Needy timer
                    case "needyactivationtime":
                        if (needyActivationTimeSpecified)
                        {
                            errorMessage = "needy activation time specified multiple times";
                            goto error;
                        }

                        needyActivationTimeSpecified = true;

                        var needyActivationTime = int.Parse(match.Groups["Value"].Value);
                        if (needyActivationTime < 0)
                        {
                            errorMessage = "invalid needy activation time";
                            goto error;
                        }

                        if (currentBomb != null) currentBomb.TimeBeforeNeedyActivation = needyActivationTime;
                        else defaultNeedyActivationTime = needyActivationTime;
                        break;

                    // Widgets
                    case "widgets":
                        if (widgetCountSpecified)
                        {
                            errorMessage = "widget count specified multiple times";
                            goto error;
                        }

                        widgetCountSpecified = true;
                        int widgetCount;
                        if (!int.TryParse(match.Groups["Value"].Value, out widgetCount))
                        {
                            errorMessage = "invalid widget count";
                            goto error;
                        }

                        if (widgetCount < 0)
                        {
                            errorMessage = "invalid widget count";
                            goto error;
                        }

                        if (currentBomb != null) currentBomb.OptionalWidgetCount = widgetCount;
                        else defaultWidgetCount = widgetCount;
                        break;

                    // Front only
                    case "frontonly":
                        if (currentBomb != null) currentBomb.FrontFaceOnly = true;
                        else defaultFrontOnly = true;
                        break;

                    // No pacing
                    case "nopacing":
                        if (bombs != null && currentBomb != null)
                        {
                            errorMessage = "nopacing cannot be a bomb-level setting";
                            goto error;
                        }
                        else mission.PacingEventsEnabled = false;

                        break;

                    // Factory mode
                    case "factory":
                        if (bombs != null && currentBomb != null)
                        {
                            errorMessage = "Factory mode cannot be a bomb-level setting";
                            goto error;
                        }
                        else if (factoryMode.HasValue)
                        {
                            errorMessage = "factory mode specified multiple times";
                            goto error;
                        }
                        else
                        {
                            int i;
                            for (i = 0; i < FactoryModes.Length; ++i)
                            {
                                if (FactoryModes[i].Equals(match.Groups["Value"].Value,
                                    StringComparison.InvariantCultureIgnoreCase)) break;
                            }

                            if (i >= FactoryModes.Length)
                            {
                                errorMessage = "invalid factory mode";
                                goto error;
                            }

                            factoryMode = i;
                        }

                        break;
                }
            }
            // Module pools
            else if (match.Groups["ID"].Success)
            {
                // Break on unmatched quote
                if (match.Groups["Error"].Success)
                {
                    errorMessage = "unclosed quote";
                    goto error;
                }

                // Create a new bomb
                if (bombs == null)
                {
                    if (currentBomb == null)
                    {
                        newBomb();

                        // Setup the bomb
                        if (defaultTime.HasValue)
                        {
                            timeSpecified = true;
                            currentBomb.TimeLimit = defaultTime.Value;
                        }

                        if (defaultStrikes.HasValue)
                        {
                            strikesSpecified = true;
                            currentBomb.NumStrikes = defaultStrikes.Value;
                        }

                        if (defaultNeedyActivationTime.HasValue)
                        {
                            needyActivationTimeSpecified = true;
                            currentBomb.TimeBeforeNeedyActivation = defaultNeedyActivationTime.Value;
                        }

                        if (defaultWidgetCount.HasValue)
                        {
                            widgetCountSpecified = true;
                            currentBomb.OptionalWidgetCount = defaultWidgetCount.Value;
                        }
                    }
                }
                else
                {
                    if (currentBomb == null)
                    {
                        errorMessage = "Unexpected module pool";
                        goto error;
                    }
                }

                // Create the module pool
                var pool = new KMComponentPool
                {
                    Count = match.Groups["Count"].Success ? int.Parse(match.Groups["Count"].Value) : 1,
                    ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                    ModTypes = new List<string>()
                };
                if (pool.Count <= 0)
                {
                    errorMessage = "Invalid module pool count";
                    goto error;
                }

                // Parse module IDs
                var list = match.Groups["ID"].Value.Replace("\"", "").Replace("'", "").Trim();
                if(list.StartsWith("mode:"))
					continue;
                switch (list)
                {
                    // Module groups
                    case "ALL_SOLVABLE":
                        pool.AllowedSources =
                            KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods;
                        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
                        break;
                    case "ALL_NEEDY":
                        pool.AllowedSources =
                            KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods;
                        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_NEEDY;
                        break;
                    case "ALL_VANILLA":
                        pool.AllowedSources = KMComponentPool.ComponentSource.Base;
                        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
                        break;
                    case "ALL_MODS":
                        pool.AllowedSources = KMComponentPool.ComponentSource.Mods;
                        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
                        break;
                    case "ALL_VANILLA_NEEDY":
                        pool.AllowedSources = KMComponentPool.ComponentSource.Base;
                        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_NEEDY;
                        break;
                    case "ALL_MODS_NEEDY":
                        pool.AllowedSources = KMComponentPool.ComponentSource.Mods;
                        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_NEEDY;
                        break;

                    // Individual module IDs
                    default:
                        foreach (var id in list.Split(',', '+').Select(s => s.Trim()))
                        {
                            switch (id)
                            {
                                // Vanilla modules
                                case "WireSequence":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.WireSequence);
                                    break;
                                case "Wires":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Wires);
                                    break;
                                case "WhosOnFirst":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.WhosOnFirst);
                                    break;
                                case "Simon":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Simon);
                                    break;
                                case "Password":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Password);
                                    break;
                                case "Morse":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Morse);
                                    break;
                                case "Memory":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Memory);
                                    break;
                                case "Maze":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Maze);
                                    break;
                                case "Keypad":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Keypad);
                                    break;
                                case "Venn":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.Venn);
                                    break;
                                case "BigButton":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.BigButton);
                                    break;
                                case "NeedyCapacitor":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.NeedyCapacitor);
                                    break;
                                case "NeedyVentGas":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.NeedyVentGas);
                                    break;
                                case "NeedyKnob":
                                    pool.ComponentTypes.Add(KMComponentPool.ComponentTypeEnum.NeedyKnob);
                                    break;

                                // Modded modules
                                default:
                                    pool.ModTypes.Add(id);
                                    break;
                            }
                        }

                        break;
                }

                pools.Add(pool);
            }
            // Multiple Bombs starting point
            else if (match.Groups["Open"].Success)
            {
                if (currentBomb != null)
                {
                    errorMessage = "Unexpected '('";
                    goto error;
                }

                bombRepeatCount = match.Groups["Count"].Success ? int.Parse(match.Groups["Count"].Value) : 1;
                if (bombRepeatCount <= 0)
                {
                    errorMessage = "Invalid bomb repeat count";
                    goto error;
                }

                if (bombs == null) bombs = new List<KMGeneratorSetting>();
                newBomb();
            }
            // Multiple Bombs ending point
            else if (match.Groups["Close"].Success)
            {
                if (currentBomb == null)
                {
                    errorMessage = "Unexpected ')'";
                    goto error;
                }

                validateBomb();
                for (; bombRepeatCount > 0; --bombRepeatCount) bombs.Add(currentBomb);
                currentBomb = null;
            }
        }

        // Check if no modules were provided
        if (bombs == null)
        {
            if (currentBomb == null)
            {
                errorMessage = "No solvable modules";
                goto error;
            }

            validateBomb();
            mission.GeneratorSetting = currentBomb;
        }
        else if (bombs.Count == 0)
        {
            errorMessage = "No solvable modules";
        }

        // Handle parsing error
        error:
        if (errorMessage != null)
        {
            Debug.LogErrorFormat("[DMG] Error: {0}", errorMessage);
            throw new ParseException(errorMessage);
        }

        // Convert bombs to JSON for Multiple Bombs
        if (bombs != null)
        {
            mission.GeneratorSetting = bombs[0];
            if (bombs.Count > 1)
            {
                mission.GeneratorSetting.ComponentPools.Add(new KMComponentPool
                {
                    Count = bombs.Count - 1, ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                    ModTypes = new List<string> {"Multiple Bombs"}
                });
                for (int i = 1; i < bombs.Count; ++i)
                {
                    // if (bombs[i] != mission.GeneratorSetting)
                    mission.GeneratorSetting.ComponentPools.Add(new KMComponentPool
                    {
                        Count = 1, ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                        ModTypes = new List<string>
                            {string.Format("Multiple Bombs:{0}:{1}", i, JsonConvert.SerializeObject(bombs[i]))}
                    });
                }
            }
        }

        // Apply factory mode
        if (factoryMode.HasValue)
            mission.GeneratorSetting.ComponentPools.Add(new KMComponentPool
            {
                Count = factoryMode.Value, ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                ModTypes = new List<string> {"Factory Mode"}
            });

        return mission;
    }

    public static string GetDmgString(KMMission mission)
    {
        string result = "";

        // Mission doc comments
        result += string.Format("//// {0}\n{1}\n\n",
            mission.DisplayName,
            mission.Description.Trim().Split('\n').Select(line => "/// " + line.Trim())
                .Join("\n"));

        // No pacing events
        if (!mission.PacingEventsEnabled)
        {
            result += "nopacing\n";
        }

        // Bomb settings
        result += GetBombSettingsString(mission.GeneratorSetting) + "\n";

        // Pools
        string pools = "";
        List<KMGeneratorSetting> otherBombs = new List<KMGeneratorSetting>();
        mission.GeneratorSetting.ComponentPools.ForEach(pool =>
        {
            // Factory mode
            if (pool.ModTypes.Contains("Factory Mode"))
            {
                string factoryMode;
                if (pool.Count < 0 || pool.Count >= FactoryModes.Length)
                {
                    Debug.LogError("Invalid factory mode");
                    return;
                }

                factoryMode = FactoryModes[pool.Count];

                result += string.Format("factory:{0}\n", factoryMode);
            }
            // Multiple Bombs bomb count
            else if (pool.ModTypes.Contains("Multiple Bombs"))
            {
                // ignore
            }
            // Multiple Bombs bomb JSON
            else if (pool.ModTypes.Count == 1 && pool.ModTypes[0].StartsWith("Multiple Bombs"))
            {
                otherBombs.Add(
                    JsonConvert.DeserializeObject<KMGeneratorSetting>(pool.ModTypes[0].Split(new[] {':'}, 3)[2]));
            }
            // Normal pool
            else
            {
                pools += GetPoolDmgString(pool) + "\n";
            }
        });

        // Determine whether to output as Multiple Bombs or as a single bomb
        if (otherBombs.Count != 0)
        {
            // Create bomb sets
            var bombSets = new List<Pair<int, KMGeneratorSetting>>
            {
                new Pair<int, KMGeneratorSetting> {First = 1, Second = mission.GeneratorSetting}
            };

            // Group equal bombs
            foreach (var bomb in otherBombs)
            {
                if (bomb.SameMission(bombSets.Last().Second))
                {
                    bombSets.Last().First++;
                }
                else
                {
                    bombSets.Add(new Pair<int, KMGeneratorSetting> {First = 1, Second = bomb});
                }
            }

            result = result.TrimEnd();

            // Output each bomb
            result = bombSets.Aggregate(result, (current, bomb) =>
                current + ("\n\n" + (bomb.First != 1 ? bomb.First + "*" : "")
                                  + "(\n" + Indent((GetBombSettingsString(bomb.Second, mission.GeneratorSetting)
                                                    + "\n\n" + bomb.Second.ComponentPools.FilterPools()
                                                        .Select(GetPoolDmgString).Join("\n")).Trim()) + "\n)"));
        }
        else
        {
            result += "\n" + pools.TrimEnd();
        }

        return result;
    }

    private static string GetBombSettingsString(KMGeneratorSetting bomb, KMGeneratorSetting compare = null)
    {
        var result = "";

        // Time limit
        if (compare == null || Math.Abs(bomb.TimeLimit - compare.TimeLimit) > 0.1f)
        {
            var t = TimeSpan.FromSeconds(bomb.TimeLimit);
            result += string.Format("{0:D2}:{1:D2}:{2:D2}\n",
                t.Hours,
                t.Minutes,
                t.Seconds);
        }

        // Strike count
        if (compare == null || bomb.NumStrikes != compare.NumStrikes)
        {
            result += bomb.NumStrikes + "X\n";
        }

        // Needy activation
        if (compare == null || bomb.TimeBeforeNeedyActivation != compare.TimeBeforeNeedyActivation)
        {
            result += string.Format("needyactivationtime:{0}\n", bomb.TimeBeforeNeedyActivation);
        }

        // Widget count
        if (compare == null || bomb.OptionalWidgetCount != compare.OptionalWidgetCount)
        {
            result += string.Format("widgets:{0}\n", bomb.OptionalWidgetCount);
        }

        // Front only
        if (bomb.FrontFaceOnly && (compare == null || bomb.FrontFaceOnly != compare.FrontFaceOnly))
        {
            result += "frontonly\n";
        }

        return result.TrimEnd();
    }

    private static string GetPoolDmgString(KMComponentPool pool)
    {
        string source;

        // Handle special pools
        switch (pool.SpecialComponentType)
        {
            // Normal pool
            case KMComponentPool.SpecialComponentTypeEnum.None:
                var modules = new List<string>(pool.ModTypes);
                if (pool.ComponentTypes != null)
                {
                    modules.AddRange(pool.ComponentTypes.Select(vanillaModule => vanillaModule.ToString()));
                }

                source = modules.Select(s => s.IndexOfAny(new[] { ',', ' ', '+' }) != -1 ? '"' + s + '"' : s).Join(",");
                break;
            
            // Needy pool
            case KMComponentPool.SpecialComponentTypeEnum.ALL_NEEDY:
                if ((pool.AllowedSources & KMComponentPool.ComponentSource.Base &
                     KMComponentPool.ComponentSource.Mods) != 0)
                {
                    source = "ALL_NEEDY";
                }
                else if ((pool.AllowedSources & KMComponentPool.ComponentSource.Mods) != 0
                         && (pool.AllowedSources & KMComponentPool.ComponentSource.Base) == 0)
                {
                    source = "ALL_MODS_NEEDY";
                }
                else
                {
                    source = "ALL_VANILLA_NEEDY";
                }

                break;
            
            // Solvable pool
            case KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE:
                if ((pool.AllowedSources & KMComponentPool.ComponentSource.Base &
                     KMComponentPool.ComponentSource.Mods) != 0)
                {
                    source = "ALL_SOLVABLE";
                }
                else if ((pool.AllowedSources & KMComponentPool.ComponentSource.Mods) != 0
                         && (pool.AllowedSources & KMComponentPool.ComponentSource.Base) == 0)
                {
                    source = "ALL_MODS";
                }
                else
                {
                    source = "ALL_VANILLA";
                }

                break;
            default:
                return null;
        }

        // Create pool string
        return pool.Count == 1 ? source : string.Format("{0}*{1}", pool.Count, source);
    }

    private static string Indent(string str, int spaces = 2)
    {
        return str.Split('\n').Select(line => line.PadLeft(line.Length + spaces, ' ')).Join("\n");
    }
}

internal static class DmgExtensions
{
    private static readonly Regex FileNumberRegex = new Regex(@"^(?<Num>\d+)\..+");

    public static bool SameMission(this KMGeneratorSetting a, KMGeneratorSetting b)
    {
        return Mathf.RoundToInt(a.TimeLimit) == Mathf.RoundToInt(b.TimeLimit)
               && a.NumStrikes == b.NumStrikes
               && a.TimeBeforeNeedyActivation == b.TimeBeforeNeedyActivation
               && a.FrontFaceOnly == b.FrontFaceOnly
               && a.OptionalWidgetCount == b.OptionalWidgetCount
               && a.ComponentPools.FilterPools().SamePools(b.ComponentPools.FilterPools());
    }

    private static bool SamePools(this ICollection<KMComponentPool> a, IList<KMComponentPool> b)
    {
        return a.Count == b.Count && a.Select((pool, i) => pool.SamePool(b[i])).All(x => x);
    }

    private static bool SamePool(this KMComponentPool a, KMComponentPool b)
    {
        return a.Count == b.Count
               && a.AllowedSources == b.AllowedSources
               && a.SpecialComponentType == b.SpecialComponentType
               && a.ComponentTypes.ListEquals(b.ComponentTypes)
               && a.ModTypes.ListEquals(b.ModTypes);
    }

    private static bool ListEquals<T>(this ICollection<T> a, IList<T> b)
    {
        return a.Count == b.Count && a.Select((e, i) => e.Equals(b[i])).All(x => x);
    }

    public static List<KMComponentPool> FilterPools(this IEnumerable<KMComponentPool> pools)
    {
        return pools.Where(pool =>
            !pool.ModTypes.Contains("Factory Mode") && !pool.ModTypes.Contains("Multiple Bombs") &&
            (pool.ModTypes.Count != 1 || !pool.ModTypes[0].StartsWith("Multiple Bombs"))).ToList();
    }

    public static IOrderedEnumerable<string> OrderPaths(this IEnumerable<string> paths)
    {
        return paths.OrderBy(path =>
        {
            var matches = FileNumberRegex.Matches(Path.GetFileName(path));
            if (matches.Count == 0)
                return "zzzz" + Path.GetFileName(path);
            return string.Format(@"{0:D4}", int.Parse(matches[0].Groups["Num"].Value));
        });
    }
}
