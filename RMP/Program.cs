using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RMP.Properties;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace RMP
{
    class Program
    {
        static string baseloc;
        [STAThread]
        static void Main(string[] args)
        {
            bool israel = false;
            if (args.Length < 1 || args.Contains("help"))
            {
                Console.WriteLine(@"Sanae's Disappearing Rabbit Patcher v1.1
A game patcher for Oh Jeez, Oh No, My Rabbits Are Gone!
Command Line Usage: RMP.exe <path to data.win> [patches]
The path to the data.win file should be where you downloaded your game.
If you downloaded it on Steam, look up how to get to a game's local files.
Patches available are:
- all - default option, applies all patches, all must be the first patch in the command
  all other supplied patches will be ignored
  if you want to specific patches, please type them instead of using all
- speedrun - the main patch for speedrunning, some patches have a dependency on this one
- intro - skips the intro in speedrunning mode (depends on speedrun)
- clock - adds a clock to the top left of the window (depends on speedrun)
- frame - press P to change maximum frame speed between 1000 and 60
- debug - adds several debug options to the game (disabled by default) (see github readme for more info)
- color - a cute little bunny color changer, randomized on room (re)entry
- nosave - prevents savepoints from being created (disabled by default)
- decomp - dumps all game code to a folder inside current directory (disabled by default)
- everything - runs *every* patch/action, unlike all, which just runs all non-severe actions/patches
- every - everything except for the decomp action
- ever - every but no decomp and no debug
");
                if (args.Length == 1 && args[0] != "help")
                {
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    return;
                }
                var ofd = new OpenFileDialog();
                ofd.Title = "Locate your data.win file";
                ofd.Filter = "data.win|data.win";
                DialogResult dr = ofd.ShowDialog();
                if (dr == DialogResult.Cancel)
                {
                    return;
                }
                baseloc = ofd.FileName;
                israel = true;
            }else baseloc = args[0];
            List<string> actions;
            {
                if (israel || args.Length == 1 || (args[1] == "all")) actions = new List<string>(){
                    "speedrun",
                    "frame",
                    "color",
                    "clock",
                    "intro"
                };
                else if (args[1] == "everything")
                {
                    actions = new List<string>()
                    {
                        "speedrun",
                        "frame",
                        "color",
                        "clock",
                        "intro",
                        "debug",
                        "nosave",
                        "decomp"
                    };
                }
                else if (args[1] == "every")
                {
                    actions = new List<string>()
                    {
                        "speedrun",
                        "frame",
                        "color",
                        "clock",
                        "intro",
                        "debug",
                        "nosave"
                    };
                }
                else
                {
                    actions = new List<string>(){
                        ""
                    };
                    actions.AddRange(args.Skip(1));
                }
            }
            if (!Path.HasExtension(baseloc) || !Directory.Exists(Path.GetDirectoryName(baseloc)))
            {
                Console.Error.WriteLine("Invalid data.win path!\n"+baseloc);
                return;
            }
            baseloc = Path.GetDirectoryName(baseloc)+"\\";
            if (!File.Exists(baseloc + "data.win.orig")) if (File.Exists(baseloc + "data.win"))File.Move(baseloc + "data.win", baseloc + "data.win.orig");
            else{
                Console.Error.WriteLine("There's no data.win in this folder, and there's no data.win.orig!");
                return;
            }
            File.Delete(baseloc + "data.win");
            File.Copy(baseloc + "data.win.orig", baseloc + "data.win");
            FileStream fileStream = File.Open(baseloc + "data.win", FileMode.Open);
            UndertaleData data = UndertaleIO.Read(fileStream);
            fileStream.Close();
            File.Delete(baseloc + "data.win");
            if (data.GeneralInfo.DisplayName.Content != "My Rabbits Are Gone")
            {
                Console.Error.WriteLine("This data.win is not from Oh Jeez, Oh No, My Rabbits Are Gone\nPlease provide that one instead.");
                File.Copy(baseloc + "data.win.orig", baseloc + "data.win");
                return;
            }
            
            List<string> usedActions = new List<string>();
            var gi = new UndertaleGlobalInit();
            var code = new UndertaleCode();
            code.ReplaceGML("global.patched = true",data);
            code.Name = data.Strings.MakeString("gml_global_set_patched");
            gi.Code = code;
            bool speedran = false;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i] != "")
                {
                    if (usedActions.Contains(actions[i])) continue;
                    usedActions.Add(actions[i]);//prevent repatching actions that are already patched
                    Console.WriteLine(actions[i]);
                }
                switch (actions[i])
                {
                    case "speedrun":
                        speedran = true;
                        SpeedrunPatches(data);
                        break;
                    case "debug":
                        DebugPatches(data);
                        break;
                    case "clock":
                        if (!speedran) YUNORAN();
                        ClockPatches(data);
                        break;
                    case "intro":
                        if (!speedran) YUNORAN();
                        IntroPatch(data);
                        break;
                    case "nosave":
                        if (!speedran) YUNORAN();
                        SavePointPatch(data);
                        break;
                    case "decomp":
                        Directory.CreateDirectory("./decomp/");
                        for(int ia = 0; ia < data.Code.Count; ia++)
                        {
                            UndertaleCode uc = data.Code[ia];
                            Console.WriteLine("Writing " + uc.Name.Content);
                            try
                            {
                                File.WriteAllText(baseloc+"/decomp/" + uc.Name.Content, Decompiler.Decompile(uc, new DecompileContext(data, false)));
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine("failed! "+e.Message);
                            }
                            Console.WriteLine("Wrote " + uc.Name.Content);
                        }
                        break;
                    case "frame":
                        FramecapRemover(data);
                        break;
                    case "color":
                        ColorRandomizer(data);
                        break;
                    case "multiplayer":
                        Console.WriteLine("maybe one day, not today 😭");
                        break;
                    case "":
                        break;
                    default:
                        Console.Error.WriteLine($"Invalid action {actions[i]}\n Run RMP.exe with no arguments to see proper usage of the program.");
                        File.Copy(baseloc + "data.win.orig", baseloc + "data.win");
                        return;
                }
            }
            
            UndertaleIO.Write(File.Open(baseloc + "data.win", FileMode.OpenOrCreate),data);
            Console.WriteLine("Wrote data.win! A new option has been added to the main menu.");
        }

        private static void YUNORAN()
        {
            File.Copy(baseloc + "data.win.orig", baseloc + "data.win");
            Console.Error.WriteLine(@"You need to run the speedrun patch first, so put it right after the data.win location instead
so that the dependent patches can work without issue.

Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(1);
        }

        private static void IntroPatch(UndertaleData data)
        {
            Console.WriteLine("for skipping intros in normal saves on version 1.1.0.2 and up, see SETTINGS>GAME OPTIONS>Speedrun mode");
            UndertaleCode ea = data.Code.ByName("gml_Script_start_new_game");
            ReplaceInGML("loadroom = rm_intro", "if (global.CurrentFile == \"savedfile4.sav\")loadroom = rm_house;\n" +
                "else loadroom = rm_intro", ea, data);
        }
        public static void DebugPatches(UndertaleData data)
        {
            UndertaleCode ee = data.Code.ByName("gml_Object_obj_menus_Create_0");
            ReplaceInGML("1, 2], [\"\", 9],", "1, 2], [\"EXTRAS\", 1, 9], ", ee, data);
            ee.UpdateAddresses();

            data.Code.ByName("gml_Object_obj_constant_Draw_64").AppendGML(RabbitRunCode.constBruhwer, data);
        }

        public static void SpeedrunPatches(UndertaleData data)
        {
            UndertaleScript setboi = new UndertaleScript();
            setboi.Name = data.Strings.MakeString("set_speedrun_category");
            setboi.Code = new UndertaleCode();
            setboi.Code.Name = data.Strings.MakeString("gml_Script_set_speedrun_category");
            setboi.Code.ReplaceGML(RabbitRunCode.set_speedrun_category, data);
            data.Code.Add(setboi.Code);
            data.Scripts.Add(setboi);

            UndertaleScript sprun = new UndertaleScript();
            sprun.Name = new UndertaleString("menu_speedrun_script");
            data.Strings.Add(sprun.Name);
            sprun.Code = new UndertaleCode();
            data.Code.Add(sprun.Code);
            sprun.Code.Name = new UndertaleString("gml_Script_menu_speedrun_script");
            data.Strings.Add(sprun.Code.Name);
            sprun.Code.ReplaceGML(RabbitRunCode.menu_speedrun_script, data);
            sprun.Code.UpdateAddresses();
            data.Scripts.Add(sprun);

            UndertaleCode ae = data.Code.ByName("gml_Script_setfile");
            ae.AppendGML(RabbitRunCode.gml_Script_setfile, data);
            ae.UpdateAddresses();

            UndertaleCode ee = data.Code.ByName("gml_Object_obj_mainmenus_Create_0");
            ReplaceInGML("GAME\", 1, 8],", "GAME\", 1, 8], [\"SPEEDBUN\", 1, 19], ", ee, data);
            ee.UpdateAddresses();
            ReplaceInGML("i = 0",RabbitRunCode.speedrunMenuInit,ee,data);

            UndertaleCode ie = data.Code.ByName("gml_Script_cKeys_beginstep");
            ie.AppendGML(RabbitRunCode.tasBeginStepInput, data);

            UndertaleCode oe = data.Code.ByName("gml_Object_obj_init_Create_0");
            oe.AppendGML(@"global.playRun = false;
global.watchRun = false;
global.speedrunning = true;
global.inrun = false;
global.onehun = false;//one hundred percent
global.allbun = false;//all cuties
global.anyper = false;//any percent", data);

            UndertaleCode ue = data.Code.ByName("gml_Script_SaveStringToFile");
            ue.ReplaceGML(RabbitRunCode.saveStringFile, data);

            UndertaleSprite mico = data.Sprites.ByName("spr_menuicons");
            UndertaleSprite.TextureEntry te = new UndertaleSprite.TextureEntry();
            UndertaleTexturePageItem ti = mico.Textures[1].Texture;
            UndertaleTexturePageItem to = data.Sprites.ByName("spr_antibunidle").Textures[0].Texture;
            te.Texture = new UndertaleTexturePageItem();
            te.Texture.TargetX = ti.TargetX;
            te.Texture.TargetY = ti.TargetY;
            te.Texture.SourceX = to.SourceX;
            te.Texture.SourceY = to.SourceY;
            te.Texture.BoundingHeight = ti.BoundingHeight;
            te.Texture.BoundingWidth = ti.BoundingWidth;
            te.Texture.SourceWidth = 16;
            te.Texture.TargetWidth = 16;
            te.Texture.SourceHeight= 15;
            te.Texture.TargetHeight= 15;
            te.Texture.TexturePage = to.TexturePage;
            data.TexturePageItems.Add(te.Texture);
            mico.Textures.Insert(2,te);
        }
        public static void SavePointPatch(UndertaleData data)
        {
            UndertaleCode ye = data.Code.ByName("gml_Object_objpre_savepoint_Create_0");
            ye.ReplaceGML(RabbitRunCode.savepointdestroyer, data);
        }
        public static void ClockPatches(UndertaleData data)
        {
            UndertaleGameObject go = new UndertaleGameObject();
            go.Name = data.Strings.MakeString("obj_clock");
            go.Sprite = null;
            go.Persistent = true;
            go.CollisionShape = 0;
            go.Depth = -1;
            go.Awake = true;
            go.Visible = true;
            CreateEvent(RabbitRunCode.coinstants, data, go, EventType.Create, 0u);
            CreateEvent(RabbitRunCode.yote, data, go, EventType.Other, (uint)EventSubtypeOther.RoomEnd);
            CreateEvent(RabbitRunCode.rabstart,
                data, go, EventType.Other, (uint)EventSubtypeOther.RoomStart);
            CreateEvent(RabbitRunCode.constantDrawer, data, go, EventType.Draw, (uint)EventSubtypeDraw.DrawGUIEnd);
            CreateEvent(RabbitRunCode.constantStepper, data, go, EventType.Step, (uint)EventSubtypeStep.Step);
            CreateEvent(RabbitRunCode.doneThisShit, data, go, EventType.Other, (uint)EventSubtypeOther.User0);
            data.GameObjects.Add(go);
            data.Code.ByName("gml_Script_pausegame").AppendGML("instance_activate_object(obj_clock);", data);
            UndertaleCode c = data.Code.ByName("gml_Script_goto_mainmenu");
            c.AppendGML(RabbitRunCode.gohomebyebye, data);
            UndertaleCode endingCutscene = data.Code.ByName("gml_RoomCC_rm_n4_760_Create");
            ReplaceInGML("t_scene_info = [", @"t_scene_info = [[cutscene_checkiflist, obj_constant.flagList, 191, 1, 1],[cutscene_activate_userevent, obj_clock,0],", data.Code.ByName("gml_RoomCC_rm_n4_760_Create"), data);
            ReplaceInGML("t_scene_info = [", @"t_scene_info = [[cutscene_activate_userevent, obj_clock,0],", data.Code.ByName("gml_RoomCC_rm_n5_33_Create"), data);
            var house = data.Rooms.ByName("rm_init");
            UndertaleRoom.GameObject rogo = new UndertaleRoom.GameObject
            {
                ObjectDefinition = go,
                InstanceID = 108990u,
                GMS2_2_2 = true
            };

            house.GameObjects.Add(rogo);
            house.Layers.Single((layer)=>layer.LayerName.Content == "Instances").InstancesData.Instances.Add(rogo);
            
        }

        public static void FramecapRemover(UndertaleData data)
        {
            var cst = data.GameObjects.ByName("obj_constant");
            cst.EventHandlerFor(EventType.Create, data.Strings, data.Code, data.CodeLocals).AppendGML(RabbitRunCode.speedycreate, data);
            cst.EventHandlerFor(EventType.Step, data.Strings, data.Code, data.CodeLocals).AppendGML(RabbitRunCode.speedystepper, data);
        }

        public static void ColorRandomizer(UndertaleData data)
        {
            //set random image_blend in obj_rabbitb, rabbit drawing doesn't depend on some random values
            data.GameObjects.ByName("obj_rabbitb").EventHandlerFor(EventType.Create,data.Strings,data.Code,data.CodeLocals).AppendGML(RabbitRunCode.colorBlend, data);
        }

        public static void ReplaceInGML(string str1, string str2,UndertaleCode code, UndertaleData data)
        {
            string decomp = Decompiler.Decompile(code, new DecompileContext(data, false));
            decomp = decomp.Replace(str1,str2);
            code.ReplaceGML(decomp, data);
            code.UpdateAddresses();
        }
        public static void CreateEvent(string gml, UndertaleData data, UndertaleGameObject obj, EventType type,uint subtype)
        {
            UndertaleCode c = obj.EventHandlerFor(type,subtype, data.Strings, data.Code, data.CodeLocals);
            c.ReplaceGML(gml, data);
        }
    }
}

/**
 * Legend
 * ---------
 * 📌 = low priority, but on the lis
 * TODO
 * ---------
 * (probably not happening) Add speedrun rule qualification checks
 * ✔ Add a speedrun clock
 * ✔ Make the speedrun clock start when you enter the house at the start of the game
 * ?? Make the speedrun clock end when you enter the house in the sprint home
 * ✔ Skip the intro cutscene
 * 📌Creating a replay system
 * Turning the replay system into a tas tool
 * Randomize bunny colors or transformations midgame for funnies
 * Rabbit counter
 * 100% Run Management
 * - Skip endings 
 * - Do second run
 * 
 * Multiplayer?????
 */
