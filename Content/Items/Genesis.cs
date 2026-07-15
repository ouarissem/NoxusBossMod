using System.Collections.Generic;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables;
using CalamityMod.Tiles.Furniture.CraftingStations;
using NoxusBossMod.Content.Bosses;
using NoxusBossMod.Content.NPCs;
using NoxusBossMod.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBossMod.Content.Items
{
    public class Genesis : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 34;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = 16;
        }

        public override bool CanUseItem(Player player) =>
            !NPC.AnyNPCs(ModContent.NPCType<NoxusEgg>()) && !NPC.AnyNPCs(ModContent.NPCType<EntropicGod>()) && !NPC.AnyNPCs(ModContent.NPCType<NoxusEggCutscene>());

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int noxusID = player.altFunctionUse == 2 || !WorldSaveSystem.HasDefeatedEgg ? ModContent.NPCType<NoxusEgg>() : ModContent.NPCType<EntropicGod>();

                // Simple and guaranteed method that works in all game modes
                NPC.SpawnOnPlayer(player.whoAmI, noxusID);
            }
            return true;
        }

        public override bool AltFunctionUse(Player player) => WorldSaveSystem.HasDefeatedEgg;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
        
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddTile<DraedonsForge>().
                AddIngredient(ItemID.StoneBlock, 50).
                AddIngredient(ModContent.ItemType<ShadowspecBar>(), 10).
                AddIngredient(ModContent.ItemType<Rock>()).
                Register();
        }
    }
}