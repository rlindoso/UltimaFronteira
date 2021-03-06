using System;
using Server.Mobiles;
using Server.Engines.Harvest;

namespace Server.Items
{
    [FlipableAttribute(0x9E7E, 0x9E7F)]
    public class RockHammer : Pickaxe
    {
        public override int LabelNumber { get { return 1124598; } }
    
        [Constructable]
        public RockHammer()
            : this(500)
        {
        }

        [Constructable]
        public RockHammer(int uses)
            : base()
        {
            Name = "Picareta Marretadora de Pedras";
            this.Weight = 5.0;
            this.UsesRemaining = uses;
        }

        public RockHammer(Serial serial)
            : base(serial)
        {
        }

        public override HarvestSystem HarvestSystem
        {
            get
            {
                return Mining.System;
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
