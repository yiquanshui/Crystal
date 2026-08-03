using Server.MirEnv;

namespace Server.MirDatabase
{
    public class AuctionInfo
    {
        protected static Env Env => Env.Main;

        public ulong AuctionID;

        public readonly UserItem Item;
        public DateTime ConsignmentDate;
        public readonly uint Price;
        public uint CurrentBid;

        public int SellerIndex;
        public int CurrentBuyerIndex;
        public CharacterInfo? SellerInfo, CurrentBuyerInfo;

        public bool Expired, Sold;

        public readonly MarketItemType ItemType;


        public AuctionInfo(CharacterInfo info, UserItem item, uint price, MarketItemType itemType)
        {
            AuctionID = ++Env.NextAuctionID;
            SellerIndex = info.Index;
            SellerInfo = info;
            ConsignmentDate = Env.Now;
            Item = item;
            Price = price;
            ItemType = itemType;

            if (itemType == MarketItemType.Auction)
            {
                CurrentBid = Price;
            }
        }

        public AuctionInfo(BinaryReader reader, int version, int customVersion)
        {
            AuctionID = reader.ReadUInt64();

            Item = new UserItem(reader, version, customVersion);
            ConsignmentDate = DateTime.FromBinary(reader.ReadInt64());
            Price = reader.ReadUInt32();
            SellerIndex = reader.ReadInt32();
            Expired = reader.ReadBoolean();
            Sold = reader.ReadBoolean();

            if (version > 79)
            {
                ItemType = (MarketItemType)reader.ReadByte();

                CurrentBid = reader.ReadUInt32();

                if (CurrentBid < Price)
                    CurrentBid = Price;

                CurrentBuyerIndex = reader.ReadInt32();
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(AuctionID);

            Item.Save(writer);
            writer.Write(ConsignmentDate.ToBinary());
            writer.Write(Price);

            writer.Write(SellerIndex);

            writer.Write(Expired);
            writer.Write(Sold);

            writer.Write((byte)ItemType);
            writer.Write(CurrentBid);
            writer.Write(CurrentBuyerIndex);
        }

        private string GetSellerLabel(bool userMatch)
        {
            if (SellerInfo == null)
            {
                return string.Empty;
            }
            switch (ItemType)
            {
                case MarketItemType.GameShop:
                    return "";
                case MarketItemType.Consign:
                    return userMatch ? (Sold ? "Sold" : (Expired ? "Expired" : "For Sale")) : SellerInfo.Name;
                case MarketItemType.Auction:
                    return userMatch ? (Sold ? "Sold" : (Expired ? "Expired" : CurrentBid > Price ? "Bid Met" : "No Bid")) : SellerInfo.Name;
            }

            return string.Empty;
        }

        public ClientAuction CreateClientAuction(bool userMatch)
        {
            return new ClientAuction
            {
                AuctionID = AuctionID,
                Item = Item,
                Seller = GetSellerLabel(userMatch),
                Price = ItemType == MarketItemType.Auction ? CurrentBid : Price,
                ConsignmentDate = ConsignmentDate,
                ItemType = ItemType
            };
        }
    }
}
