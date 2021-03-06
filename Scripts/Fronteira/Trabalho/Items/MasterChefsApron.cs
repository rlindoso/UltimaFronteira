namespace Server.Items
{
    public class MasterChefsApron : FullApron
    {
        private int _Bonus;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Bonus { get { return _Bonus; } set { _Bonus = value; InvalidateProperties(); } }

        public override int LabelNumber { get { return 1157228; } } // Master Chef's Apron

        [Constructable]
        public MasterChefsApron()
        {
            Hue = 1990;
            Name = "Avental do Trabalhador";
            while(_Bonus == 0)
                _Bonus = BaseTalisman.GetRandomExceptional();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add("+"+_Bonus+"% Crafting Exceptional"); // ~1_NAME~ Exceptional Bonus: ~2_val~%
        }

        public MasterChefsApron(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(_Bonus);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            _Bonus = reader.ReadInt();
        }
    }
}
