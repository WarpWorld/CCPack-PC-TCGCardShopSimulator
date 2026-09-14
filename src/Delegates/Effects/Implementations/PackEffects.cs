#nullable disable
using ConnectorLib.JSON;

namespace CrowdControl.Delegates.Effects.Implementations;

// Instant effects. Each class forwards to the matching method in GameActions, which contains the actual
// game logic (kept from the previous version of the mod). Effect IDs must match the Crowd Control pack file.

[Effect(["lights"])]
public class ToggleLightsEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.ToggleLights(request);
}

[Effect(["spawn"])]
public class SpawnCustomerEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.SpawnCustomer(request);
}

[Effect(["spawnsmelly"])]
public class SpawnCustomerSmellyEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.SpawnCustomerSmelly(request);
}

[Effect(["allsmelly"])]
public class AllSmellyCustomersEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.AllSmellyCustomers(request);
}

[Effect(["open_store", "close_store", "renamestore"])]
public class ShopControlsEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.ShopControls(request);
}

[Effect(["unlockwh"])]
public class UnlockWarehouseEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.UnlockWarehouse(request);
}

[Effect(["upgradewh"])]
public class UpgradeWarehouseEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.UpgradeWarehouse(request);
}

[Effect(["upgradestore"])]
public class UpgradeStoreEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.UpgradeStore(request);
}

[Effect(["teleport"])]
public class TeleportPlayerEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.TeleportPlayer(request);
}

[Effect(["give_100", "give_1000", "give_10000"])]
public class GiveMoneyEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.GiveMoney(request);
}

[Effect(["take_100", "take_1000", "take_10000"])]
public class TakeMoneyEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.TakeMoney(request);
}

[Effect(["giveempty"])]
public class GiveEmptyEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.GiveEmpty(request);
}

[Effect(["giveplayerhugeempty"])]
public class SendHugeEmptyEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.SendHugeEmpty(request);
}

[Effect(["give_common_pack_(64)", "give_common_box_(8)", "give_rare_pack_(64)", "give_rare_box_(8)", "give_epic_pack_(64)", "give_epic_box_(8)", "give_legend_pack_(64)", "give_legend_box_(8)", "give_deck_box_red", "give_deck_box_green", "give_deck_box_blue", "give_deck_box_yellow", "give_destiny_common_pack_(64)", "give_destiny_common_box_(8)", "give_destiny_rare_pack_(64)", "give_destiny_rare_box_(8)", "give_destiny_epic_pack_(64)", "give_destiny_epic_box_(8)", "give_destiny_legend_pack_(64)", "give_destiny_legend_box_(8)", "give_cleanser_(16)", "give_cleanser_(32)", "give_collection_book", "give_d20_dice_red", "give_d20_dice_blue", "give_d20_dice_black", "give_d20_dice_white", "give_piggya_plushie", "give_golema_plushie", "give_starfisha_plushie", "give_bata_plushie", "give_toonz_plushie", "give_burpig_figurine", "give_inferhog_figurine", "give_blazoar_plushie", "give_decimite_figurine", "give_meganite_figurine", "give_giganite_statue", "give_trickstar_figurine", "give_princestar_figurine", "give_kingstar_plushie", "give_lunight_figurine", "give_vampicant_figurine", "give_dracunix_figurine", "give_drilceros_action_figure", "give_bonfiox_plushie", "give_premium_collection_book", "give_fire_battle_deck", "give_earth_battle_deck", "give_water_battle_deck", "give_wind_battle_deck", "give_fire_destiny_deck", "give_earth_destiny_deck", "give_water_destiny_deck", "give_wind_destiny_deck", "give_card_sleeves_(clear)", "give_card_sleeves_(tetramon)", "give_card_sleeves_(fire)", "give_card_sleeves_(earth)", "give_card_sleeves_(water)", "give_card_sleeves_(wind)", "give_playmat_(clamigo)", "give_playmat_(duel)", "give_playmat_(drilceros)", "give_playmat_(drakon)", "give_playmat_(the_four_dragons)", "give_playmat_(dracunix)", "give_playmat_(wispo)", "give_playmat_(gigatronx_evo)", "give_playmat_(tetramon)", "give_playmat_(kyrone)", "give_playmat_(fire)", "give_playmat_(earth)", "give_playmat_(wind)", "give_playmat_(lunight)", "give_playmat_(water)", "give_ascension_pack_(64)", "give_playmat_(gigatronx)", "give_playmat_(katengu_black)", "give_playmat_(katengu_white)", "give_playmat_gray", "give_playmat_green", "give_playmat_purple", "give_playmat_yellow", "give_magnetic_holder", "give_card_preserver", "give_pocket_pages", "give_card_holder", "give_toploader", "give_penny_sleeves", "give_collectors_album", "give_tower_deckbox", "give_evo_blazoar_statue", "give_evo_kingstar_statue", "give_necromansters", "give_mafia_works", "give_claim!", "give_system_gate_#1", "give_system_gate_#2", "send_comic_1", "send_comic_2", "send_comic_3", "send_comic_4", "send_comic_5", "send_comic_6", "send_comic_7", "send_comic_8", "send_comic_9", "send_comic_10", "send_comic_11", "send_comic_12", "send_comic_13", "send_comic_14"])]
public class GiveItemEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.GiveItem(request);
}

[Effect(["giveatplayer_common_pack_(64)", "giveatplayer_common_box_(8)", "giveatplayer_rare_pack_(64)", "giveatplayer_rare_box_(8)", "giveatplayer_epic_pack_(64)", "giveatplayer_epic_box_(8)", "giveatplayer_legend_pack_(64)", "giveatplayer_legend_box_(8)", "giveatplayer_deck_box_red", "giveatplayer_deck_box_green", "giveatplayer_deck_box_blue", "giveatplayer_deck_box_yellow", "giveatplayer_destiny_common_pack_(64)", "giveatplayer_destiny_common_box_(8)", "giveatplayer_destiny_rare_pack_(64)", "giveatplayer_destiny_rare_box_(8)", "giveatplayer_destiny_epic_pack_(64)", "giveatplayer_destiny_epic_box_(8)", "giveatplayer_destiny_legend_pack_(64)", "giveatplayer_destiny_legend_box_(8)", "giveatplayer_cleanser_(16)", "giveatplayer_cleanser_(32)", "giveatplayer_collection_book", "giveatplayer_d20_dice_red", "giveatplayer_d20_dice_blue", "giveatplayer_d20_dice_black", "giveatplayer_d20_dice_white", "giveatplayer_piggya_plushie", "giveatplayer_golema_plushie", "giveatplayer_starfisha_plushie", "giveatplayer_bata_plushie", "giveatplayer_toonz_plushie", "giveatplayer_burpig_figurine", "giveatplayer_inferhog_figurine", "giveatplayer_blazoar_plushie", "giveatplayer_decimite_figurine", "giveatplayer_meganite_figurine", "giveatplayer_giganite_statue", "giveatplayer_trickstar_figurine", "giveatplayer_princestar_figurine", "giveatplayer_kingstar_plushie", "giveatplayer_lunight_figurine", "giveatplayer_vampicant_figurine", "giveatplayer_dracunix_figurine", "giveatplayer_drilceros_action_figure", "giveatplayer_bonfiox_plushie", "giveatplayer_premium_collection_book", "giveatplayer_fire_battle_deck", "giveatplayer_earth_battle_deck", "giveatplayer_water_battle_deck", "giveatplayer_wind_battle_deck", "giveatplayer_fire_destiny_deck", "giveatplayer_earth_destiny_deck", "giveatplayer_water_destiny_deck", "giveatplayer_wind_destiny_deck", "giveatplayer_card_sleeves_(clear)", "giveatplayer_card_sleeves_(tetramon)", "giveatplayer_card_sleeves_(fire)", "giveatplayer_card_sleeves_(earth)", "giveatplayer_card_sleeves_(water)", "giveatplayer_card_sleeves_(wind)", "giveatplayer_playmat_(clamigo)", "giveatplayer_playmat_(duel)", "giveatplayer_playmat_(drilceros)", "giveatplayer_playmat_(drakon)", "giveatplayer_playmat_(the_four_dragons)", "giveatplayer_playmat_(dracunix)", "giveatplayer_playmat_(wispo)", "giveatplayer_playmat_(gigatronx_evo)", "giveatplayer_playmat_(tetramon)", "giveatplayer_playmat_(kyrone)", "giveatplayer_playmat_(fire)", "giveatplayer_playmat_(earth)", "giveatplayer_playmat_(wind)", "giveatplayer_playmat_(lunight)", "giveatplayer_playmat_(water)", "giveatplayer_ascension_pack_(64)", "giveatplayer_playmat_(gigatronx)", "giveatplayer_playmat_(katengu_black)", "giveatplayer_playmat_(katengu_white)", "giveatplayer_playmat_gray", "giveatplayer_playmat_green", "giveatplayer_playmat_purple", "giveatplayer_playmat_yellow", "giveatplayer_magnetic_holder", "giveatplayer_card_preserver", "giveatplayer_pocket_pages", "giveatplayer_card_holder", "giveatplayer_toploader", "giveatplayer_penny_sleeves", "giveatplayer_collectors_album", "giveatplayer_tower_deckbox", "giveatplayer_evo_blazoar_statue", "giveatplayer_evo_kingstar_statue", "giveatplayer_necromansters", "giveatplayer_mafia_works", "giveatplayer_claim!", "giveatplayer_system_gate_#1", "giveatplayer_system_gate_#2"])]
public class GiveItemAtPlayerEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.GiveItemAtPlayer(request);
}

[Effect(["badreview", "goodreview"])]
public class LeaveReviewEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.LeaveReview(request);
}

[Effect(["speak_heyoo"])]
public class HeyOhhEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.HeyOhh(request);
}

[Effect(["hireworker"])]
public class HireWorkerEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.HireWorker(request);
}

[Effect(["fireworker"])]
public class FireWorkerEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.FireWorker(request);
}

[Effect(["throwitem"])]
public class ThrowItemEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.ThrowItem(request);
}

[Effect(["furniture_small_cabinet", "furniture_small_metal_rack", "furniture_play_table", "furniture_small_personal_shelf", "furniture_single_sided_shelf", "furniture_card_table", "furniture_small_warehouse_shelf", "furniture_small_card_display", "furniture_auto_scent_m100", "furniture_workbench", "furniture_double_sided_shelf", "furniture_big_warehouse_shelf", "furniture_checkout_counter", "furniture_auto_scent_g500", "furniture_card_display_table", "furniture_big_personal_shelf", "furniture_vintage_card_table", "furniture_wide_shelf", "furniture_huge_personal_shelf", "furniture_auto_scent_t1000", "furniture_big_card_display", "furniture_pack_machine_s", "furniture_pack_machine_m", "furniture_pack_machine_l", "furniture_2x2_cabinet", "furniture_trash_bin", "furniture_empty_box_storage", "furniture_wall_display_case", "furniture_card_projector_s", "furniture_card_projector_m", "furniture_card_projector_l", "furniture_bulk_donation_box", "furniture_card_storage_shelf", "furniture_tournament_prize_shelf", "furniture_corner_shelf", "furniture_box_shelf", "furniture_tetramon_shelf"])]
public class GiveItemFurnitureEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.GiveItemFurniture(request);
}

[Effect(["spawn_bread"])]
public class SpawnBreadEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.SpawnBread(request);
}

[Effect(["spawn_milk"])]
public class SpawnMilkEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.SpawnMilk(request);
}

[Effect(["event-hype-train"])]
public class SpawnHypeTrainEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.SpawnHypeTrain(request);
}

// Auto open packs: instant. The effect refuses (retry) while the player is busy, including while a
// previous auto-open is still running, so it needs no countdown of its own.
[Effect(["openpack_common_pack", "openpack_rare_pack", "openpack_epic_pack", "openpack_legend_pack", "openpack_destiny_common_pack", "openpack_destiny_rare_pack", "openpack_destiny_epic_pack", "openpack_destiny_legend_pack", "openpack_ascension_pack"])]
public class OpenCardPackEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    public override EffectResponse Start(EffectRequest request) => GameActions.OpenCardPack(request);
}
