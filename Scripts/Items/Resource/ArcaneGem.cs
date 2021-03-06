using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Items
{
    public class ArcaneGem : Item, ICommodity
    {
        public const int DefaultArcaneHue = 2117;
        [Constructable]
        public ArcaneGem()
            : base(0x1EA7)
        {
            this.Stackable = Core.ML;
            this.Weight = 1.0;
        }

        public ArcaneGem(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description { get { return LabelNumber; } }
        bool ICommodity.IsDeedable { get { return true; } }

        public override string DefaultName
        {
            get
            {
                return "gema arcana";
            }
        }
        public static bool ConsumeCharges(Mobile from, int amount)
        {
            List<Item> items = from.Items;
            int avail = 0;

            for (int i = 0; i < items.Count; ++i)
            {
                Item obj = items[i];

                if (obj is IArcaneEquip)
                {
                    IArcaneEquip eq = (IArcaneEquip)obj;

                    if (eq.IsArcane)
                        avail += eq.CurArcaneCharges;
                }
            }

            if (avail < amount)
                return false;

            for (int i = 0; i < items.Count; ++i)
            {
                Item obj = items[i];

                if (obj is IArcaneEquip)
                {
                    IArcaneEquip eq = (IArcaneEquip)obj;

                    if (eq.IsArcane)
                    {
                        if (eq.CurArcaneCharges > amount)
                        {
                            eq.CurArcaneCharges -= amount;
                            break;
                        }
                        else
                        {
                            amount -= eq.CurArcaneCharges;
                            eq.CurArcaneCharges = 0;
                        }
                    }
                }
            }

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.BeginTarget(2, false, TargetFlags.None, new TargetCallback(OnTarget));
                from.SendMessage("Onde deseja usar a gema?");
            }
        }

        public int GetChargesFor(Mobile m)
        {
            int v = (int)(m.Skills[SkillName.Tailoring].Value / 5);

            if (v < 16)
                return 16;
            else if (v > 24)
                return 24;

            return v;
        }

        public void OnTarget(Mobile from, object obj)
        {
            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (obj is IArcaneEquip && obj is Item)
            {
                Item item = (Item)obj;
                CraftResource resource = CraftResource.None;

                if (item is BaseClothing)
                    resource = ((BaseClothing)item).Resource;
                else if (item is BaseArmor)
                    resource = ((BaseArmor)item).Resource;
                else if (item is BaseWeapon) // Sanity, weapons cannot recieve gems...
                    resource = ((BaseWeapon)item).Resource;

                IArcaneEquip eq = (IArcaneEquip)obj;

                if (!item.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    return;
                }
                /*else if (item.LootType == LootType.Blessed)
                {
                    from.SendMessage("You can only use this on exceptionally crafted robes, thigh boots, cloaks, or leather gloves.");
                    return;
                }
                else if (resource != CraftResource.None && resource != CraftResource.RegularLeather)
                {
                    from.SendLocalizedMessage(1049690); // Arcane gems can not be used on that type of leather.
                    return;
                }*/

                int charges = this.GetChargesFor(from);

                if (eq.IsArcane)
                {
                    if (eq.CurArcaneCharges >= eq.MaxArcaneCharges)
                    {
                        from.SendMessage("O item ja esta carregado.");
                    }
                    else
                    {
                        if (eq.CurArcaneCharges <= 0)
                            item.Hue = DefaultArcaneHue;

                        if ((eq.CurArcaneCharges + charges) > eq.MaxArcaneCharges)
                            eq.CurArcaneCharges = eq.MaxArcaneCharges;
                        else
                            eq.CurArcaneCharges += charges;

                        from.SendMessage("Voce recarregou o item.");
                        if (this.Amount <= 1)
                            this.Delete();
                        else
                            this.Amount--;
                    }
                }
                else if (from.Skills[SkillName.Tailoring].Value >= 80.0)
                {
                    bool isExceptional = false;

                    if (item is BaseClothing)
                        isExceptional = (((BaseClothing)item).Quality == ItemQuality.Exceptional);
                    else if (item is BaseArmor)
                        isExceptional = (((BaseArmor)item).Quality == ItemQuality.Exceptional);
                    else if (item is BaseWeapon)
                        isExceptional = (((BaseWeapon)item).Quality == ItemQuality.Exceptional);

                    if (isExceptional)
                    {
                        if (item is BaseClothing)
                        {
                            ((BaseClothing)item).Quality = ItemQuality.Normal;
                            ((BaseClothing)item).Crafter = from;
                        }
                        else if (item is BaseArmor)
                        {
                            ((BaseArmor)item).Quality = ItemQuality.Normal;
                            ((BaseArmor)item).Crafter = from;
                            ((BaseArmor)item).PhysicalBonus = ((BaseArmor)item).FireBonus = ((BaseArmor)item).ColdBonus = ((BaseArmor)item).PoisonBonus = ((BaseArmor)item).EnergyBonus = 0; // Is there a method to remove bonuses?
                        }
                        else if (item is BaseWeapon) // Sanity, weapons cannot recieve gems...
                        {
                            ((BaseWeapon)item).Quality = ItemQuality.Normal;
                            ((BaseWeapon)item).Crafter = from;
                        }

                        eq.CurArcaneCharges = eq.MaxArcaneCharges = charges;

                        item.Hue = DefaultArcaneHue;

                        from.SendMessage("You enhance the item with your gem.");
                        if (this.Amount <= 1)
                            this.Delete();
                        else
                            this.Amount--;
                    }
                    else
                    {
                        from.SendMessage("Voce pode usar isto apenas em items excepcionais.");
                    }
                }
                else
                {
                    from.SendMessage("Voce nao tem skill em tailoring suficiente.");
                }
            }
            else
            {
                from.SendMessage("Voce so pode usar isto em sobretudos, botas, capas ou luvas de couro excepcionais.");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
