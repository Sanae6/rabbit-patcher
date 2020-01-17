using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace RMP
{
    static class RabbitRunCode
    {
        public const string tasBeginStepInput = @"
        if (global.playRun){
            
        }else if (global.watchRun){

        }";
        public const string menu_speedrun_script = @"
        if (!instance_exists(obj_transition))
        {
            global.speedrunning = true;
            global.playRun = true;
            setfile(4)
            loadfile()
            gamestart_load()
        }        
        ";
        public const string gml_Script_setfile =
            "var file = argument[0];" +
            "if (file == 1){global.CurrentFile = \"savedfile.sav\";global.CurrentSave = \"savedgame.sav\"}" +
            "if (file == 2){global.CurrentFile = \"savedfile2.sav\";global.CurrentSave = \"savedgame2.sav\"}" +
            "if (file == 3){global.CurrentFile = \"savedfile3.sav\";global.CurrentSave = \"savedgame3.sav\"}" +
            "if (file == 4){global.CurrentFile = \"savedfile4.sav\";global.CurrentSave = \"savedgame4.sav\"}";
        public const string saveStringFile = @"
var _filename = argument0;
var _string = argument1;
var _buffer = buffer_create((string_byte_length(_string) + 1), buffer_fixed, 1);
if (string_count(" + "\"4\""+@",_filename) > 0)exit;
buffer_write(_buffer, buffer_string, _string);
buffer_save(_buffer, _filename);
buffer_delete(_buffer);";
        public const string savepointdestroyer = "if (global.CurrentFile == \"savedfile4.sav\")instance_destroy()";
        public const string yote =
@"
if (lastroom == rm_house && room != rm_mainmenu && global.inrun == false){
    lastroom = -1;
    global.inrun = true;
    seconds = 0;
    minutes = 0;
    hours = 0;
    timetext = """";
    tiem = 0;
} else if (room == rm_ending){
    seconds = 0;
    minutes = 0;
    hours = 0;
    global.inrun = false;
    showtime = (room == rm_ending)
    beenhome = false;
}
";
        public const string constantStepper =
@"
if ((global.inrun || showtime) && !global.GamePaused || (beenhome && room != rm_house)) {
    if (beenhome && !global.GamePaused && global.inrun)tiem+=delta_time/1000000;
    seconds = tiem;
    minutes = floor(seconds / 60);
    hours = floor(minutes / 60);
    timetext = string(hours)" + "+\":\"+string_replace(string_format(minutes%60,2,0),\" \",\"0\")+\":\"+string_replace(string_format(seconds%60,2,3),\" \",\"0\")" +
 "}";
        public const string constantDrawer =
@"
if (showtime){
    draw_set_font(fnt_babyblocksmono)
    draw_set_color(0x00000000)
    draw_set_valign(fa_top)
    var xpos = view_xview[0]+3
    var ypos = view_yview[0]+4
    draw_text_transformed((xpos - 1), ypos, timetext, 0.5, 0.5, 0)
    draw_text_transformed((xpos + 1), ypos, timetext, 0.5, 0.5, 0)
    draw_text_transformed(xpos, ypos-1, timetext, 0.5, 0.5, 0)
    draw_text_transformed(xpos, ypos+1, timetext, 0.5, 0.5, 0)
    draw_set_color(0x00FFFFFF)
    draw_text_transformed(xpos, ypos, timetext, 0.5, 0.5, 0)
}
";
        public const string gohomebyebye =
@"
if (global.inrun){
    global.inrun = false;
    showtime = false;
}
";
        public const string coinstants = "beenhome = false;showtime = false;timetext=\"\";tiem=0;";
        public const string speedycreate = "superspeed = false;";
        public const string speedystepper = @"
if (keyboard_check_pressed(ord(""P""))){
    superspeed = !superspeed;
    game_set_speed(superspeed ? 1000:60,gamespeed_fps);
}";
    }
}
