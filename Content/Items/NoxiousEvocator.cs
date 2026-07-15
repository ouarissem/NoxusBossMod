using CalamityMod.Items;
using CalamityMod.Rarities;
using NoxusBossMod.Content.Bosses;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBossMod.Content.Items
{
    public class NoxiousEvocator : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = Item.buyPrice(platinum: 2, gold: 50);
            Item.rare = 16;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                NoxusFumes.CreateIllusions(player);
        }

        public override void UpdateVanity(Player player) => NoxusFumes.CreateIllusions(player);
    }
}