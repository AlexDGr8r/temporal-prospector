using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TemporalProspector
{
    public class ItemTemporalProspectingPick : Item
    {

        private const string RESOURCE_TAG = "resource";

        private SimpleParticleProperties _particlesHeld;
        private SkillItem[] _toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            _particlesHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), 
                new Vec3d(), new Vec3d(), new Vec3f(), 
                new Vec3f(), 4f, 0.0f, 0.5f, 0.75f)
            {
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f),
                addLifeLength = 0.5f,
                WindAffected = false,
                WithTerrainCollision = false,
                VertexFlags = 220,
                SelfPropelled = true
            };
            
            _toolModes = ObjectCacheUtil.GetOrCreate(api, "temporalPickToolModes", () =>
            {
                SkillItem[] skillItemArray = {
                    new SkillItem
                    {
                        Code = new AssetLocation("shortradius"),
                        Name = Lang.Get("temporalprospector:temporal-prospecting-shortradius")
                    },
                    new SkillItem
                    {
                        Code = new AssetLocation("mediumradius"),
                        Name = Lang.Get("temporalprospector:temporal-prospecting-mediumradius")
                    },
                    new SkillItem
                    {
                        Code = new AssetLocation("longradius"),
                        Name = Lang.Get("temporalprospector:temporal-prospecting-longradius")
                    }
                };

                if (api is ICoreClientAPI capi)
                {
                    skillItemArray[0].WithIcon(capi, (cr, x, y, w, h, rgba) => 
                        DrawCircle(cr, x, y, w, h, rgba, 1D / 3D));
                    skillItemArray[1].WithIcon(capi, (cr, x, y, w, h, rgba) => 
                        DrawCircle(cr, x, y, w, h, rgba, 2D / 3D));
                    skillItemArray[2].WithIcon(capi, (cr, x, y, w, h, rgba) => 
                        DrawCircle(cr, x, y, w, h, rgba, 1));
                }

                return skillItemArray;
            });
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot,
            BlockSelection blockSel, float dropQuantityMultiplier = 1f)
        {
            var radius = 15; // short radius
            switch (GetToolMode(itemslot, null, blockSel))
            {
                case 1: // Medium radius
                    radius = 30;
                    break;
                case 2: // Long radius
                    radius = 60;
                    break;
            }

            string resource = itemslot.Itemstack.Attributes.GetString(RESOURCE_TAG);
            if (resource != null)
            {
                ProspectArea(world, byEntity, blockSel, radius, resource);
            }
            else if (byEntity is EntityPlayer player)
            {
                var byPlayer = world.PlayerByUid(player.PlayerUID);
                if (byPlayer is IServerPlayer serverPlayer)
                {
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, 
                        Lang.Get("temporalprospector:no-resource-selected"), EnumChatType.Notification);
                }
            }

            if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
            {
                DamageItem(world, byEntity, itemslot, radius / 3);
            }

            return true;
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            foreach (var iSlot in allInputslots)
            {
                switch (iSlot.Itemstack?.Item)
                {
                    case ItemProspectingPick _:
                    case ItemTemporalProspectingPick _:
                        var durability = iSlot.Itemstack.Attributes.GetInt("durability");
                        if (durability > 0)
                        {
                            outputSlot.Itemstack.Attributes.SetInt("durability", durability);
                        }
                        break;
                    case ItemNugget _:
                    case ItemOre _:
                    case ItemGem _:
                        outputSlot.Itemstack.Attributes.SetString(RESOURCE_TAG, iSlot.Itemstack.Item.Variant["ore"]);
                        break;
                }
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            var resource = itemStack.Attributes.GetString(RESOURCE_TAG, "none");
            return Lang.GetMatching(Code?.Domain + ":" + ItemClass.Name() + "-" + Code?.Path + "-" + resource);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            // Simply copied from base method, but stripped down slightly for some stuff that is not necessary
            // This is purely so that we can create our own custom item description based on item attributes
            ItemStack itemstack = inSlot.Itemstack;
            string key = Code?.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" +
                         Code?.Path + "-" + itemstack.Attributes.GetString(RESOURCE_TAG, "none");
            string matching = Lang.GetMatching(key);
            string str1 = matching != key ? matching + "\n" : "";
            StringBuilder stringBuilder1 = dsc;
            int num1;
            string str2;
            if (!withDebugInfo)
            {
                str2 = "";
            }
            else
            {
                num1 = Id;
                str2 = "Id: " + num1 + "\n";
            }

            stringBuilder1.Append(str2);
            dsc.Append(withDebugInfo ? "Code: " + Code + "\n" : "");
            int durability = GetDurability(itemstack);
            if (durability > 1)
                dsc.AppendLine(Lang.Get("Durability: {0} / {1}",
                    itemstack.Attributes.GetInt("durability", durability), durability));
            if (MiningSpeed != null && MiningSpeed.Count > 0)
            {
                dsc.AppendLine(Lang.Get("Tool Tier: {0}", ToolTier));
                dsc.Append(Lang.Get("item-tooltip-miningspeed"));
                int num2 = 0;
                foreach (KeyValuePair<EnumBlockMaterial, float> keyValuePair in MiningSpeed)
                {
                    if (keyValuePair.Value >= 1.1)
                    {
                        if (num2 > 0)
                            dsc.Append(", ");
                        dsc.Append(Lang.Get(keyValuePair.Key.ToString()) + " " + keyValuePair.Value.ToString("#.#") +
                                   "x");
                        ++num2;
                    }
                }

                dsc.Append("\n");
            }

            if (GetAttackPower(itemstack) > 0.5)
            {
                dsc.AppendLine(Lang.Get("Attack power: -{0} hp",
                    (object)this.GetAttackPower(itemstack).ToString("0.#")));
                dsc.AppendLine(Lang.Get("Attack tier: {0}", ToolTier));
            }

            if (GetAttackRange(itemstack) > (double)GlobalConstants.DefaultAttackRange)
                dsc.AppendLine(Lang.Get("Attack range: {0} m",
                    GetAttackRange(itemstack).ToString("0.#")));
            
            if (str1.Length > 0 && dsc.Length > 0)
                dsc.Append("\n");
            dsc.Append(str1);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return _toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => 
            Math.Min(_toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }
        
        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            for (var index = 0; _toolModes != null && index < _toolModes.Length; ++index)
            {
                _toolModes[index]?.Dispose();
            }
        }

        protected virtual void ProspectArea(IWorldAccessor world, Entity byEntity, BlockSelection blockSel, int radius, String resourceType)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            {
                byPlayer = world.PlayerByUid(player.PlayerUID);
            }
                
            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            block.OnBlockBroken(world, blockSel.Position, byPlayer, 0.0f);
            if (!block.Code.Path.StartsWith("rock") && !block.Code.Path.StartsWith("ore") ||
                !(byPlayer is IServerPlayer serverPlayer))
            {
                return;
            }

            BlockPos blockPos = blockSel.Position.Copy();
            int numFound = 0;

            api.World.BlockAccessor.WalkBlocks(blockPos.AddCopy(radius, radius, radius),
                blockPos.AddCopy(-radius, -radius, -radius),
                (nblock, bp) =>
                {
                    if (nblock.BlockMaterial == EnumBlockMaterial.Ore && nblock.Variant.ContainsKey("type"))
                    {
                        if (nblock.Variant["type"].ToLower().Contains(resourceType))
                        {
                            numFound++;
                            SpawnParticles(world, blockPos.ToVec3d().Add(0.5D, 0.5D, 0.5D),
                                bp.ToVec3d().Add(0.5D, 0.5D, 0.5D));
                        }
                    }
                });

            string msg = Lang.Get("temporalprospector:found-" + resourceType + "-nodes-within-radius", 
                numFound, radius);
            serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
        }
        
        private void SpawnParticles(IWorldAccessor world, Vec3d pos, Vec3d endPos)
        {
            int h = 110 + world.Rand.Next(15);
            int v = 100 + world.Rand.Next(50);
            _particlesHeld.MinPos = pos;
            _particlesHeld.MinVelocity = endPos.ToVec3f() - pos.ToVec3f();
            _particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v));
            _particlesHeld.MinSize = 0.2f;
            _particlesHeld.ParticleModel = EnumParticleModel.Quad;
            _particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
            _particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));
            world.SpawnParticles(_particlesHeld);
        }

        private static void DrawCircle(Context cr, int x, int y, float width, float height, double[] rgba, double scale)
        {
            cr.SetSourceRGB(rgba[0], rgba[1], rgba[2]);
            cr.Translate(24, 24);
            cr.Scale(scale, scale);
            cr.Arc(0, 0, 18, 0, 2*Math.PI);
            cr.StrokePreserve();
            cr.Fill();
        }

    }
}