using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Network;
using Server.Mobiles;
using Server.Gumps;

namespace Server.Items
{
    public class SandstoneFireplaceSouthAddonExp : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new SandstoneFireplaceSouthDeedExp();
            }
        }

        [Constructable]
        public SandstoneFireplaceSouthAddonExp()
        {
            AddonComponent ac = null;
            ac = new

            AddonComponent(0x45F);
            ac.Name = "Sandstone Fireplace";
            AddComponent(ac, 0, 0, 0);

            ac = new AddonComponent(0x482);
            ac.Name = "Sandstone Fireplace";
            AddComponent(ac, -1, 0, 0);
             
        }

        public SandstoneFireplaceSouthAddonExp(Serial serial)
            : base(serial)
        {
        }

        public override void OnComponentUsed(AddonComponent ac, Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
                from.SendMessage("Muito longe!");
            else
            {
                if (ac.ItemID == 0x47B)
                {
                    ac.ItemID = 0x45F;
                    Effects.PlaySound(from.Location, from.Map, 0x4B9);
                    from.SendMessage("Voce apagou o fogo!");
                }
                else if (ac.ItemID == 0x45F)
                {
                    Container pack = from.Backpack;

                    if (pack == null)
                        return;

                    int res = pack.ConsumeTotal(new Type[] { typeof(Log), typeof(MatchLight) }, new int[] { 3, 1 });

                    switch (res)
                    {
                        case 0:
                            {
                                from.SendMessage("Voce precisa ter 3 toras para colocar no fogo");
                                break;
                            }
                        case 1:
                            {
                                from.SendMessage("Voce precisa ter um fosforo para ascender a fogueira");
                                break;
                            }
                        default:
                            {
                                Effects.PlaySound(from.Location, from.Map, 0x137);
                                from.SendMessage("Voce ascendeu o fogo!");
                                ac.ItemID = 0x47B;
                                ac.Light = LightType.Circle225;
                                Effects.PlaySound(from.Location, from.Map, 0x4BA);
                                break;
                            }
                    }
                }
                else
                    return;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

    }

    public class SandstoneFireplaceSouthDeedExp : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SandstoneFireplaceSouthAddonExp();
            }
        }

        public override int LabelNumber
        {
            get
            {
                return 1061845;
            }
        }// sandstone fireplace (South)

        [Constructable]
        public SandstoneFireplaceSouthDeedExp()
        {
        }

        public SandstoneFireplaceSouthDeedExp(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
