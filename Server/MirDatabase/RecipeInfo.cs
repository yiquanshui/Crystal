using Server.MirEnv;
using Server.MirObjects;

namespace Server.MirDatabase
{
    public class RecipeInfo
    {
        protected static Env Env => Env.Main;

        protected static MessageQueue MessageQueue => MessageQueue.Instance;

        public UserItem? Item;
        public List<UserItem>? Ingredients;
        public List<UserItem>? Tools;

        public List<int> RequiredFlag = [];
        public ushort? RequiredLevel = null;
        public List<int> RequiredQuest = [];
        public List<MirClass> RequiredClass = [];
        public MirGender? RequiredGender = null;

        public byte Chance = 100;
        public uint Gold = 0;

        public RecipeInfo(string name)
        {
            ItemInfo? itemInfo = Env.GetItemInfo(name);
            if (itemInfo == null)
            {
                MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CouldNotFindItem), name));
                return;
            }

            Item = Env.CreateShopItem(itemInfo, ++Env.NextRecipeID);

            LoadIngredients(name);
        }

        private void LoadIngredients(string recipe)
        {
            List<string> lines = [.. File.ReadAllLines(Path.Combine(Settings.RecipePath, recipe + ".txt"))];

            Tools = [];
            Ingredients = [];

            var mode = "ingredients";

            foreach (string line in lines.Where(line => !string.IsNullOrEmpty(line)))
            {
                if (line.StartsWith('['))
                {
                    mode = line.Substring(1, line.Length - 2).ToLower();
                    continue;
                }

                switch (mode)
                {
                    case "recipe":
                    {
                        var data = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                        if (data.Length < 2) continue;

                        switch (data[0].ToLower())
                        {
                            case "amount":
                                Item.Count = ushort.Parse(data[1]);
                                break;
                            case "chance":
                                Chance = byte.Parse(data[1]);

                                if (Chance > 100)
                                {
                                    Chance = 100;
                                }
                                break;
                            case "gold":
                                Gold = uint.Parse(data[1]);
                                break;
                            default:
                                break;
                        }
                    }
                        break;
                    case "tools":
                    {
                        var data = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                        ItemInfo? info = Env.GetItemInfo(data[0]);

                        if (info == null)
                        {
                            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CouldNotFindToolRecipe), line, recipe));
                            continue;
                        }

                        UserItem tool = Env.CreateShopItem(info, 0);
                        Tools.Add(tool);
                    }
                        break;
                    case "ingredients":
                    {
                        var data = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                        ItemInfo? info = Env.GetItemInfo(data[0]);

                        if (info == null)
                        {
                            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CouldNotFindIngredientRecipe), line, recipe));
                            continue;
                        }

                        UserItem ingredient = Env.CreateShopItem(info, 0);

                        ushort count = 1;
                        if (data.Length >= 2)
                            _ = ushort.TryParse(data[1], out count);

                        if (data.Length >= 3)
                            _ = ushort.TryParse(data[2], out ingredient.CurrentDura);

                        ingredient.Count = count > info.StackSize ? info.StackSize : count;

                        Ingredients.Add(ingredient);
                    }
                        break;
                    case "criteria":
                    {
                        var data = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                        if (data.Length < 2) continue;

                        try
                        {
                            switch (data[0].ToLower())
                            {
                                case "level":
                                    RequiredLevel = ushort.Parse(data[1]);
                                    break;
                                case "class":
                                    if (Enum.TryParse<MirClass>(data[1], true, out MirClass cls))
                                    {
                                        RequiredClass.Add(cls);
                                    }
                                    else
                                    {
                                        RequiredClass.Add((MirClass)byte.Parse(data[1]));
                                    }
                                    break;
                                case "gender":
                                    if (Enum.TryParse<MirGender>(data[1], true, out MirGender gender))
                                    {
                                        RequiredGender = gender;
                                    }
                                    else
                                    {
                                        RequiredGender = (MirGender)byte.Parse(data[1]);
                                    }
                                    break;
                                case "flag":
                                    RequiredFlag.Add(int.Parse(data[1]));
                                    break;
                                case "quest":
                                    RequiredQuest.Add(int.Parse(data[1]));
                                    break;
                            }
                        }
                        catch
                        {
                            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CouldNotParseOption), data[0], data[1]));
                            continue;
                        }
                    }
                        break;
                }
            }
        }

        public bool MatchItem(int index)
        {
            return Item != null && Item.ItemIndex == index;
        }

        public bool CanCraft(PlayerObject player)
        {
            if (RequiredLevel != null && RequiredLevel.Value > player.Level)
                return false;

            if (RequiredGender != null && RequiredGender.Value != player.Gender)
                return false;

            if (RequiredClass.Count > 0 && !RequiredClass.Contains(player.Class))
                return false;

            if (RequiredFlag.Count > 0)
            {
                if (RequiredFlag.Any(flag => !player.CharacterInfo.Flags[flag]))
                {
                    return false;
                }
            }

            return RequiredQuest.Count <= 0 || RequiredQuest.All(quest => player.CharacterInfo.CompletedQuests.Contains(quest));
        }

        public ClientRecipeInfo CreateClientRecipeInfo()
        {
            ClientRecipeInfo clientInfo = new ClientRecipeInfo
            {
                Gold = Gold,
                Chance = Chance,
                Item = Item?.Clone(),
                Tools = [.. Tools!.Select(x => x)],
                Ingredients = [.. Ingredients!.Select(x => x)]
            };

            return clientInfo;
        }
    }
}