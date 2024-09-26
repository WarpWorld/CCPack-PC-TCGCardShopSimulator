﻿using CrowdControl.Common;
using ConnectorType = CrowdControl.Common.ConnectorType;

namespace CrowdControl.Games.Packs.TCGCardShopSimulator
{

    public class TCGCardShopSimulator : SimpleTCPPack
    {
        public override string Host => "127.0.0.1";

        public override ushort Port => 51337;

        public override ISimpleTCPPack.MessageFormat MessageFormat => ISimpleTCPPack.MessageFormat.CrowdControlLegacy;

        public TCGCardShopSimulator(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler) { }

        public override Game Game { get; } = new("TCG Card Shop Simulator", "TCGCardShopSimulator", "PC", ConnectorType.SimpleTCPServerConnector);

        public override EffectList Effects => new List<Effect>
        {
                new Effect("Toggle Lights", "lights") { Description = "Toggle the Shop Lights", Category = "Misc"},
                new Effect("Spawn Customer", "spawn") { Description = "Spawns a New Customer", Category = "Spawn"},
                new Effect("Spawn Stinky Customer", "spawnsmelly") { Description = "Spawns a New Extra Stinky Customer", Category = "Spawn"},
                new Effect("Open Store", "open_store") { Description = "Opens the Players Store", Category = "Misc"},
                new Effect("Close Store", "close_store") { Description = "Closes the Players Store", Category = "Misc"},
                new Effect("Unlock Storage", "unlockwh") { Description = "Unlock The Storage room", Category = "Misc"},
                new Effect("Upgrade Storage", "upgradewh") { Description = "Upgrade the Storage Room", Category = "Misc"},
                new Effect("Make All Customers Smelly", "allsmelly") { Description = "Make All the Customers Smelly", Category = "Misc"},
                new Effect("Upgrade the Player Store", "upgradestore") { Description = "Upgrades the Player Store Size", Category = "Misc"},
                new Effect("Make the streamer do the Math", "forcemath") { Description = "Force The Player to do Math", Category = "Player"},
                new Effect("Teleport Player out of Store", "teleport") { Description = "Teleports the Player over the street", Category = "Player"},
                new Effect("Give $100", "give_100") { Description = "Give the Player $100", Category = "Money"},
                new Effect("Give $1000", "give_1000") { Description = "Give the Player $1000", Category = "Money"},
                new Effect("Give $10000", "give_10000") { Description = "Give The Player $10000", Category = "Money"},
                new Effect("Take $100", "take_100") { Description = "Take away $100 from the Player", Category = "Money"},
                new Effect("Take $1000", "take_1000") { Description = "Take Away $1000 from the Player", Category = "Money"},
                new Effect("Take $10000", "take_10000") { Description = "Take Away $10000 from the Player", Category = "Money"},
                new Effect("Force Cash Only", "forcepayment_cash") { Description = "Force all customers to pay with cash only.", Duration = 60, Category = "Payments"},
                new Effect("Force Card Only", "forcepayment_card") { Description = "Force all customers to pay with card only.", Duration = 60, Category = "Payments"},
                new Effect("Give 32 Common Packs", "give_common_pack_(32)") { Description = "Send the player a Common Pack (32)", Category = "Items"},
                new Effect("Give 64 Common Packs", "give_common_pack_(64)") { Description = "Send the player a Common Pack (64)", Category = "Items"},
                new Effect("Give Common Box (4)", "give_common_box_(4)") { Description = "Send the player a Common Box (4)", Category = "Items"},
                new Effect("Give Common Box (8)", "give_common_box_(8)") { Description = "Send the player a Common Box (8)", Category = "Items"},
                new Effect("Give Rare Pack (32)", "give_rare_pack_(32)") { Description = "Send the player a Rare Pack (32)", Category = "Items"},
                new Effect("Give Rare Pack (64)", "give_rare_pack_(64)") { Description = "Send the player a Rare Pack (64)", Category = "Items"},
                new Effect("Give Rare Box (4)", "give_rare_box_(4)") { Description = "Send the player a Rare Box (4)", Category = "Items"},
                new Effect("Give Rare Box (8)", "give_rare_box_(8)") { Description = "Send the player a Rare Box (8)", Category = "Items"},
                new Effect("Give Epic Pack (32)", "give_epic_pack_(32)") { Description = "Send the player an Epic Pack (32)", Category = "Items"},
                new Effect("Give Epic Pack (64)", "give_epic_pack_(64)") { Description = "Send the player an Epic Pack (64)", Category = "Items"},
                new Effect("Give Epic Box (4)", "give_epic_box_(4)") { Description = "Send the player an Epic Box (4)", Category = "Items"},
                new Effect("Give Epic Box (8)", "give_epic_box_(8)") { Description = "Send the player an Epic Box (8)", Category = "Items"},
                new Effect("Give Legend Pack (32)", "give_legend_pack_(32)") { Description = "Send the player a Legend Pack (32)", Category = "Items"},
                new Effect("Give Legend Pack (64)", "give_legend_pack_(64)") { Description = "Send the player a Legend Pack (64)", Category = "Items"},
                new Effect("Give Legend Box (4)", "give_legend_box_(4)") { Description = "Send the player a Legend Box (4)", Category = "Items"},
                new Effect("Give Legend Box (8)", "give_legend_box_(8)") { Description = "Send the player a Legend Box (8)", Category = "Items"},
                new Effect("Give Deck Box Red (Small Box)", "give_deck_box_red_(sm)") { Description = "Send the player a Red Deck box (Small)", Category = "Items"},
                new Effect("Give Deck Box Red (Large Box)", "give_deck_box_red") { Description = "Send the player a Red Deck Box (Large)", Category = "Items"},
                new Effect("Give Deck Box Green (Small Box)", "give_deck_box_green_(sm)") { Description = "Send the player a Green Deck Box (Small)", Category = "Items"},
                new Effect("Give Deck Box Green (Large Box)", "give_deck_box_green") { Description = "Send the player a Green Deck Box (Large)", Category = "Items"},
                new Effect("Give Deck Box Blue (Small Box)", "give_deck_box_blue_(sm)") { Description = "Send the player a Blue Deck Box (Small)", Category = "Items"},
                new Effect("Give Deck Box Blue (Large Box)", "give_deck_box_blue") { Description = "Send the player a Blue Deck Box (Large)", Category = "Items"},
                new Effect("Give Deck Box Yellow (Small Box)", "give_deck_box_yellow_(sm)") { Description = "Send the player a Yellow Deck Box (Small)", Category = "Items"},
                new Effect("Give Deck Box Yellow (Large Box)", "give_deck_box_yellow") { Description = "Send the player a Yellow Deck Box (Large)", Category = "Items"},
                new Effect("Give Destiny Common Pack (32)", "give_destiny_common_pack_(32)") { Description = "Send the player a Destiny Common Pack (32) Box", Category = "Items"},
                new Effect("Give Destiny Common Pack (64)", "give_destiny_common_pack_(64)") { Description = "Send the player a Destiny Common Pack (64) Box", Category = "Items"},
                new Effect("Give Destiny Common Box (4 Packs)", "give_destiny_common_box_(4)") { Description = "Send the player a Destiny Common Box (4)", Category = "Items"},
                new Effect("Give Destiny Common Box (8 Packs)", "give_destiny_common_box_(8)") { Description = "Send the player a Destiny Common Box (8)", Category = "Items"},
                new Effect("Give Destiny Rare Pack (32)", "give_destiny_rare_pack_(32)") { Description = "Send the player a Destiny Rare Pack (32) Box", Category = "Items"},
                new Effect("Give Destiny Rare Pack (64)", "give_destiny_rare_pack_(64)") { Description = "Send the player a Destiny Rare Pack (64) Box", Category = "Items"},
                new Effect("Give Destiny Rare Box (4 Packs)", "give_destiny_rare_box_(4)") { Description = "Send the player a Destiny Rare Box (4)", Category = "Items"},
                new Effect("Give Destiny Rare Box (8)", "give_destiny_rare_box_(8)") { Description = "Send the player a Destiny Rare Box (8)", Category = "Items"},
                new Effect("Give Destiny Epic Pack (32)", "give_destiny_epic_pack_(32)") { Description = "Send the player a Destiny Epic Pack (32) Box", Category = "Items"},
                new Effect("Give Destiny Epic Pack (64)", "give_destiny_epic_pack_(64)") { Description = "Send the player a Destiny Epic Pack (64) Box", Category = "Items"},
                new Effect("Give Destiny Epic Box (4 Packs)", "give_destiny_epic_box_(4)") { Description = "Send the player a Destiny Epic Box (4) Packs", Category = "Items"},
                new Effect("Give Destiny Epic Box (8 Packs)", "give_destiny_epic_box_(8)") { Description = "Send the player a Destiny Epic Box (8) Packs", Category = "Items"},
                new Effect("Give Destiny Legend Pack (32)", "give_destiny_legend_pack_(32)") { Description = "Send the player a Destiny Legend Pack (32) Box", Category = "Items"},
                new Effect("Give Destiny Legend Pack (64)", "give_destiny_legend_pack_(64)") { Description = "Send the player a Destiny Legend Pack (64) Box", Category = "Items"},
                new Effect("Give Destiny Legend Box (4)", "give_destiny_legend_box_(4)") { Description = "Send the player a Destiny Legend Box (4) Packs", Category = "Items"},
                new Effect("Give Destiny Legend Box (8)", "give_destiny_legend_box_(8)") { Description = "Send the player a Destiny Legend Box (8) Packs", Category = "Items"},
                new Effect("Give Cleanser (16)", "give_cleanser_(16)") { Description = "Send the player a Cleanser pack of 16", Category = "Items"},
                new Effect("Give Cleanser (32)", "give_cleanser_(32)") { Description = "Send the player a Cleanser pack of 32", Category = "Items"},
                new Effect("Give Collection Book", "give_collection_book") { Description = "Send the player a Collection Book", Category = "Items"},
                new Effect("Give D20 Red Dice", "give_d20_dice_red") { Description = "Send the player D20 Red Dice", Category = "Items"},
                new Effect("Give D20 Blue Dice", "give_d20_dice_blue") { Description = "Send the player D20 Blue Dice", Category = "Items"},
                new Effect("Give D20 Black Dice", "give_d20_dice_black") { Description = "Send the player D20 Black Dice", Category = "Items"},
                new Effect("Give D20 White Dice", "give_d20_dice_white") { Description = "Send the player D20 White Dice", Category = "Items"},
                new Effect("Give Piggy Plushie", "give_piggya_plushie") { Description = "Send the player a Piggy Plushie", Category = "Items"},
                new Effect("Give Golem Plushie", "give_golema_plushie") { Description = "Send the player a Golem Plushie", Category = "Items"},
                new Effect("Give Starfish Plushie", "give_starfisha_plushie") { Description = "Send the player a Starfish Plushie", Category = "Items"},
                new Effect("Give Bat Plushie", "give_bata_plushie") { Description = "Send the player a Bat Plushie", Category = "Items"},
                new Effect("Give Toon Plushie", "give_toonz_plushie") { Description = "Send the player a Toon Plushie", Category = "Items"},
                new Effect("Give Kingstar Plushie", "give_kingstar_plushie") { Description = "Send the player a Kingstar Plushie", Category = "Items"},
                new Effect("Give Bonfiox Plushie", "give_bonfiox_plushie") { Description = "Send the player a Bonfiox Plushie", Category = "Items"},
                new Effect("Give Burpig Figurine", "give_burpig_figurine") { Description = "Send the player a Burpig Figurine", Category = "Items"},
                new Effect("Give Inferhog Figurine", "give_inferhog_figurine") { Description = "Send the player a Inferhog Figurine", Category = "Items"},
                new Effect("Give Blazoar Plushie", "give_blazoar_plushie") { Description = "Send the player a Blazoar Plushie", Category = "Items"},
                new Effect("Give Decimite Figurine", "give_decimite_figurine") { Description = "Send the player a Decimite Figurine", Category = "Items"},
                new Effect("Give Meganite Figurine", "give_meganite_figurine") { Description = "Send the player a Meganite Figurine", Category = "Items"},
                new Effect("Give Giganite Statue", "give_giganite_statue") { Description = "Send the player a Giganite Statue", Category = "Items"},
                new Effect("Give Trickstar Figurine", "give_trickstar_figurine") { Description = "Send the player a Trickstar Figurine", Category = "Items"},
                new Effect("Give Princestar Figurine", "give_princestar_figurine") { Description = "Send the player a Princestar Figurine", Category = "Items"},
                new Effect("Give Lunight Figurine", "give_lunight_figurine") { Description = "Send the player a Lunight Figurine", Category = "Items"},
                new Effect("Give Vampicant Figurine", "give_vampicant_figurine") { Description = "Send the player a Vampicant Figurine", Category = "Items"},
                new Effect("Give Dracunix Figurine", "give_dracunix_figurine") { Description = "Send the player a Dracunix Figurine", Category = "Items"},
                new Effect("Give Drilceros Action Figure", "give_drilceros_action_figure") { Description = "Send the player a Drilceros Action Figure", Category = "Items"},
                new Effect("Give Fire Battle Deck", "give_fire_battle_deck") { Description = "Send the player a Fire Battle Deck", Category = "Items"},
                new Effect("Give Earth Battle Deck", "give_earth_battle_deck") { Description = "Send the player a Earth Battle Deck", Category = "Items"},
                new Effect("Give Water Battle Deck", "give_water_battle_deck") { Description = "Send the player a Water Battle Deck", Category = "Items"},
                new Effect("Give Wind Battle Deck", "give_wind_battle_deck") { Description = "Send the player a Wind Battle Deck", Category = "Items"},
                new Effect("Give Fire Destiny Deck", "give_fire_destiny_deck") { Description = "Send the player a Fire Destiny Deck", Category = "Items"},
                new Effect("Give Earth Destiny Deck", "give_earth_destiny_deck") { Description = "Send the player a Earth Destiny Deck", Category = "Items"},
                new Effect("Give Water Destiny Deck", "give_water_destiny_deck") { Description = "Send the player a Water Destiny Deck", Category = "Items"},
                new Effect("Give Wind Destiny Deck", "give_wind_destiny_deck") { Description = "Send the player a Wind Destiny Deck", Category = "Items"},
                new Effect("Give Clear Card Sleeves", "give_card_sleeves_(clear)") { Description = "Send the player Clear Card Sleeves", Category = "Items"},
                new Effect("Give Tetramon Style Card Sleeves", "give_card_sleeves_(tetramon)") { Description = "Send the player Tetramon Card Sleeves", Category = "Items"},
                new Effect("Give Fire Art Card Sleeves", "give_card_sleeves_(fire)") { Description = "Send the player Fire Art Card Sleeves", Category = "Items"},
                new Effect("Give Earth Art Card Sleeves", "give_card_sleeves_(earth)") { Description = "Send the player Earth Art Card Sleeves", Category = "Items"},
                new Effect("Give Water Art Card Sleeves", "give_card_sleeves_(water)") { Description = "Send the player Water Art Card Sleeves", Category = "Items"},
                new Effect("Give Wind Art Card Sleeves", "give_card_sleeves_(wind)") { Description = "Send the player Wind Art Card Sleeves", Category = "Items"},
                new Effect("Give Playmat (Clamigo)", "give_playmat_(clamigo)") { Description = "Send the player a Clamingo Playmat", Category = "Items"},
                new Effect("Give Playmat (Duel)", "give_playmat_(duel)") { Description = "Send the player a Duel Playmat", Category = "Items"},
                new Effect("Give Playmat (Drilceros)", "give_playmat_(drilceros)") { Description = "Send the player a Drilceros Playmat", Category = "Items"},
                new Effect("Give Playmat (Drakon)", "give_playmat_(drakon)") { Description = "Send the player a Drakon Playmat", Category = "Items"},
                new Effect("Give Playmat (The Four Dragons)", "give_playmat_(the_four_dragons)") { Description = "Send the player a The Four Dragons Playmat", Category = "Items"},
                new Effect("Give Playmat (Dracunix)", "give_playmat_(dracunix)") { Description = "Send the player a Dracunix Playmat", Category = "Items"},
                new Effect("Give Playmat (Wispo)", "give_playmat_(wispo)") { Description = "Send the player a Wispo Playmat", Category = "Items"},
                new Effect("Give Playmat (GigatronX Evo)", "give_playmat_(gigatronx_evo)") { Description = "Send the player a GigatronX Evo Playmat", Category = "Items"},
                new Effect("Give Playmat (Tetramon)", "give_playmat_(tetramon)") { Description = "Send the player a Tetramon Playmat", Category = "Items"},
                new Effect("Give Playmat (Kyrone)", "give_playmat_(kyrone)") { Description = "Send the player a Kyrone Playmat", Category = "Items"},
                new Effect("Give Playmat (Fire)", "give_playmat_(fire)") { Description = "Send the player a Fire Playmat", Category = "Items"},
                new Effect("Give Playmat (Earth)", "give_playmat_(earth)") { Description = "Send the player a Earth Playmat", Category = "Items"},
                new Effect("Give Playmat (Water)", "give_playmat_(water)") { Description = "Send the player a Water Playmat", Category = "Items"},
                new Effect("Give Playmat (Wind)", "give_playmat_(wind)") { Description = "Send the player a Wind Playmat", Category = "Items"},
                new Effect("Give Playmat (Lunight)", "give_playmat_(lunight)") { Description = "Send the player a Lunight Playmat", Category = "Items"},
                new Effect("High FOV", "highfov") { Description = "Set the FOV for the player higher than normal!", Duration = 30, Category = "Game FOV"},
                new Effect("Low FOV", "lowfov") { Description = "Set the FOV for the player lower, giving them tunnel vision!", Duration = 30, Category = "Game FOV"},
                new Effect("Invert X-Axis", "invertx") { Description = "Invert the X-Axis of the players controls!", Duration = 30, Category = "Axis Control"},
                new Effect("Invert Y-Axis", "inverty") { Description = "Invert the Y-Axis of the players controls!", Duration = 30, Category = "Axis Control"},

        };
    }
}
