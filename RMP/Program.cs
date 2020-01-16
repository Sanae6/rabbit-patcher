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

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: RMP.exe <path to data.win> [action]...\n" +
                    "If no actions are provided, it will patch in all of the speedrunning features" +
                    "\nExpects a data file for Oh Jeez, Oh No, My Rabbits Are Gone\n" +
                    "Example: RMP.exe \"C:\\Program Files(x86)\\Steam\\steamapps\\common\\MyRabbitsAreGone\\data.win\"\n" +
                    "Actions:\n" +
                    "  clock - Adds a speedrun clock to the top left of the screen (activates once you leave the house)\n" +
                    "  intro - Skips the intro in speedrun mode");
                return;
            }
            string baseloc = args[0];
            List<string> actions;
            if (args.Length > 1) {
                actions = args.ToList();
            }else
            {
                actions = new List<string>(){
                    "",
                    "clock",
                    "intro"
                };
            }
            if (!Path.HasExtension(baseloc) || !Directory.Exists(Path.GetDirectoryName(baseloc)))
            {
                Console.Error.WriteLine("Invalid data.win path!\n"+baseloc);
                return;
            }
            //DialogResult dr = ofd.ShowDialog();
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
                Console.Error.WriteLine("This data.win is not from the game!!");
                return;
            }
            int uei = 0;
            string[] usedActions = new string[5];
            SpeedrunPatches(data);
            for (int i = 1; i < actions.Count; i++)
            {
                if (usedActions.Contains(actions[i])) continue;
                usedActions[uei++] = actions[i];//prevent repatching actions that are already patched
                Console.WriteLine(actions[i]);
                switch (actions[i])
                {
                    case "clock":
                        ClockPatches(data);
                        break;
                    case "intro":
                        IntroPatch(data);
                        break;
                    case "decomp":
                        Directory.CreateDirectory("./decomp/");
                        for(int ia = 0; ia < data.Code.Count; ia++)
                        {
                            UndertaleCode uc = data.Code[ia];
                            Console.WriteLine("Writing " + uc.Name.Content);
                            try
                            {
                                File.WriteAllText("./decomp/" + uc.Name.Content, Decompiler.Decompile(uc, new DecompileContext(data, false)));
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine("failed! "+e.Message);
                            }
                            Console.WriteLine("Wrote " + uc.Name.Content);
                        }
                        break;
                    default:
                        Console.Error.WriteLine("Invalid action! Run RMP.exe with no arguments to see proper usage of the program.");
                        return;
                }
            }
            
            UndertaleIO.Write(File.Open(baseloc + "data.win", FileMode.OpenOrCreate),data);
            Console.WriteLine("Wrote data.win! A new option has been added to the main menu.");
        }

        private static void IntroPatch(UndertaleData data)
        {
            UndertaleCode ea = data.Code.ByName("gml_Script_start_new_game");
            ReplaceInGML("loadroom = rm_intro", "if (global.CurrentFile == \"savedfile4.sav\")loadroom = rm_house;\n" +
                "else loadroom = rm_intro", ea, data);
        }

        public static void SpeedrunPatches(UndertaleData data)
        {
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
            ReplaceInGML("GAME\", 1, 8],", "GAME\", 1, 8], [\"SPEEDBUN\", 0, " + data.Scripts.IndexOf(sprun) + "], ", ee, data);
            ee.UpdateAddresses();

            ee = data.Code.ByName("gml_Object_obj_menus_Create_0");
            ReplaceInGML("0, 171],", "0, 171], [\"EXTRAS\", 1, 8], ", ee, data);
            ee.UpdateAddresses();

            UndertaleCode ie = data.Code.ByName("gml_Script_cKeys_beginstep");
            ie.AppendGML(RabbitRunCode.tasBeginStepInput, data);

            UndertaleCode oe = data.Code.ByName("gml_Object_obj_init_Create_0");
            oe.AppendGML("global.playRun = false;\nglobal.watchRun = false;\nglobal.speedrunning = true;global.inrun = false", data);

            UndertaleCode ue = data.Code.ByName("gml_Script_SaveStringToFile");
            ue.ReplaceGML(RabbitRunCode.saveStringFile, data);

            UndertaleCode ye = data.Code.ByName("gml_Object_objpre_savepoint_Create_0");
            ye.ReplaceGML(RabbitRunCode.savepointdestroyer, data);

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
        public static void ClockPatches(UndertaleData data)
        {
            UndertaleGameObject go = new UndertaleGameObject();
            go.Name = data.Strings.MakeString("obj_clock");
            go.Sprite = null;
            go.Persistent = true;
            go.CollisionShape = 0;
            go.Awake = true;
            go.Visible = true;
            CreateEvent(RabbitRunCode.coinstants, data, go, EventType.Create, 0u);
            CreateEvent(RabbitRunCode.yote, data, go, EventType.Other, (uint)EventSubtypeOther.RoomEnd);
            CreateEvent("if (room == rm_house){showtime = true;beenhome = true;show_message(\"hi lol\")}if (room == rm_mainmenu)beenhome = false;lastroom = room;",
                data, go, EventType.Other, (uint)EventSubtypeOther.RoomStart);
            CreateEvent(RabbitRunCode.constantDrawer, data, go, EventType.Draw, (uint)EventSubtypeDraw.DrawGUI);
            CreateEvent(RabbitRunCode.constantStepper, data, go, EventType.Step, (uint)EventSubtypeStep.Step);
            data.GameObjects.Add(go);
            UndertaleCode c = data.Code.ByName("gml_Script_goto_mainmenu");
            c.AppendGML(RabbitRunCode.gohomebyebye, data);
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

        public static void ReplaceInGML(string str1, string str2,UndertaleCode code, UndertaleData data)
        {
            string decomp = Decompiler.Decompile(code, new DecompileContext(data, false));
            decomp = decomp.Replace(str1,str2);
            code.ReplaceGML(decomp, data);
        }
        public static void CreateEvent(string gml, UndertaleData data, UndertaleGameObject obj, EventType type,uint subtype)
        {
            //UndertaleGameObject.Event evt = new UndertaleGameObject.Event();
            //UndertaleGameObject.EventAction ea = new UndertaleGameObject.EventAction();
            //evt.EventSubtype = subtype;
            //ea.CodeId = new UndertaleCode();
            //ea.CodeId.Name = data.Strings.MakeString("gml_Object_" + obj.Name.Content + "_" + type.ToString() + "_" + subtype);
            //data.Code.Add(ea.CodeId);
            //UndertaleCodeLocals cl = new UndertaleCodeLocals();
            //cl.Name = data.Strings.MakeString(ea.CodeId.Name.Content);
            //data.CodeLocals.Add(cl);
            //CompileContext ctx = Compiler.CompileGMLText(gml, new CompileContext(data, ea.CodeId));
            //ea.CodeId.Replace(Assembler.Assemble(ctx.ResultAssembly, data));
            //evt.Actions.Add(ea);
            //obj.Events[(int)type].Add(evt);
            UndertaleCode c = obj.EventHandlerFor(type,subtype, data.Strings, data.Code, data.CodeLocals);
            c.ReplaceGML(gml, data);
        }
    }
}

/**
 * TODO
 * ---------
 * (probably not happening) Add speedrun rule qualification checks
 * ✔ Add a speedrun clock
 * ✔ Make the speedrun clock start when you enter the house at the start of the game
 * ?? Make the speedrun clock end when you enter the house in the sprint home
 * ✔ Skip the intro cutscene
 * Creating a replay system
 * Turning the replay system into a tas tool
 */
