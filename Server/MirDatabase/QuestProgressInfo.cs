using Server.MirObjects;
using Server.MirEnv;

namespace Server.MirDatabase
{
    public class QuestProgressInfo
    {
        protected static Env Env => Env.Main;

        public PlayerObject? Owner;

        public int Index;

        public QuestInfo? Info;

        public DateTime StartDateTime = DateTime.MinValue;
        public DateTime EndDateTime = DateTime.MaxValue;

        public List<QuestKillTaskProgress> KillTaskCount = [];
        public List<QuestItemTaskProgress> ItemTaskCount = [];
        public List<QuestFlagTaskProgress> FlagTaskSet = [];

        public List<string> TaskList = [];

        public bool IsOrphan { get; private set; }
        
        public bool Taken => StartDateTime > DateTime.MinValue;

        public bool Completed => EndDateTime < DateTime.MaxValue;

        public bool New => StartDateTime > Env.Now.AddDays(-1);


        public QuestProgressInfo(int index)
        {
            Index = index;

            Info = Env.QuestInfoList.FirstOrDefault(e => e.Index == index);

            if (Info == null)
            {
                IsOrphan = true;
                return;
            }
            
            foreach (var kill in Info.KillTasks)
            {
                KillTaskCount.Add(new QuestKillTaskProgress
                {
                    MonsterID = kill.Monster.Index,
                    Info = kill
                });
            }

            foreach (var item in Info.ItemTasks)
            {
                ItemTaskCount.Add(new QuestItemTaskProgress
                {
                    ItemID = item.Item.Index,
                    Info = item
                });
            }

            foreach (var flag in Info.FlagTasks)
            {
                FlagTaskSet.Add(new QuestFlagTaskProgress
                {
                    Number = flag.Number,
                    Info = flag
                });
            }
        }

        public QuestProgressInfo(BinaryReader reader, int version, int customVersion)
        {
            Index = reader.ReadInt32();
            Info = Env.QuestInfoList.FirstOrDefault(e => e.Index == Index);

            StartDateTime = DateTime.FromBinary(reader.ReadInt64());
            EndDateTime = DateTime.FromBinary(reader.ReadInt64());

            if (Info == null)
            {
                IsOrphan = true;

                if (version < 90)
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++) reader.ReadInt32(); // Kill counts

                    count = reader.ReadInt32();
                    for (int i = 0; i < count; i++) reader.ReadInt32(); // Item counts

                    count = reader.ReadInt32();
                    for (int i = 0; i < count; i++) reader.ReadBoolean(); // Flag states
                }
                else
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        reader.ReadInt32(); // MonsterID
                        reader.ReadInt32(); // Count
                    }

                    count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        reader.ReadInt32(); // ItemID
                        reader.ReadInt32(); // Count
                    }

                    count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        reader.ReadInt32();  // Flag Number
                        reader.ReadBoolean(); // State
                    }
                }

                return; // Skip rest of constructor if quest is missing
            }
            
            if (version < 90)
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var killCount = reader.ReadInt32();

                    if (Info.KillTasks.Count > i)
                    {
                        var progress = new QuestKillTaskProgress
                        {
                            MonsterID = Info.KillTasks[i].Monster.Index,
                            Count = killCount,
                            Info = Info.KillTasks[i]
                        };
                        KillTaskCount.Add(progress);
                    }
                }

                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var itemCount = reader.ReadInt32();
                    if (Info.ItemTasks.Count > i)
                    {
                        var progress = new QuestItemTaskProgress
                        {
                            ItemID = Info.ItemTasks[i].Item.Index,
                            Count = itemCount,
                            Info = Info.ItemTasks[i]
                        };
                        ItemTaskCount.Add(progress);
                    }
                }

                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var flagState = reader.ReadBoolean();
                    if (Info.FlagTasks.Count > i)
                    {
                        var progress = new QuestFlagTaskProgress
                        {
                            Number = Info.FlagTasks[i].Number,
                            State = flagState,
                            Info = Info.FlagTasks[i]
                        };
                        FlagTaskSet.Add(progress);
                    }
                }
            }
            else
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var progress = new QuestKillTaskProgress
                    {
                        MonsterID = reader.ReadInt32(),
                        Count = reader.ReadInt32()
                    };

                    foreach (var task in Info.KillTasks.Where(task => task.Monster.Index == progress.MonsterID))
                    {
                        progress.Info = task;
                        KillTaskCount.Add(progress);
                        break;
                    }
                }

                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var progress = new QuestItemTaskProgress
                    {
                        ItemID = reader.ReadInt32(),
                        Count = reader.ReadInt32()
                    };

                    foreach (var task in Info.ItemTasks.Where(task => task.Item.Index == progress.ItemID))
                    {
                        progress.Info = task;
                        ItemTaskCount.Add(progress);
                        break;
                    }
                }

                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var progress = new QuestFlagTaskProgress
                    {
                        Number = reader.ReadInt32(),
                        State = reader.ReadBoolean()
                    };

                    foreach (var task in Info.FlagTasks.Where(task => task.Number == progress.Number))
                    {
                        progress.Info = task;
                        FlagTaskSet.Add(progress);
                        break;
                    }
                }

                //Add any new tasks which may have been added
                foreach (var kill in Info.KillTasks)
                {
                    if (KillTaskCount.Any(x => x.MonsterID == kill.Monster.Index)) continue;

                    KillTaskCount.Add(new QuestKillTaskProgress
                    {
                        MonsterID = kill.Monster.Index,
                        Info = kill
                    });
                }

                foreach (var item in Info.ItemTasks)
                {
                    if (ItemTaskCount.Any(x => x.ItemID == item.Item.Index)) continue;

                    ItemTaskCount.Add(new QuestItemTaskProgress
                    {
                        ItemID = item.Item.Index,
                        Info = item
                    });
                }

                foreach (var flag in Info.FlagTasks)
                {
                    if (FlagTaskSet.Any(x => x.Number == flag.Number)) continue;

                    FlagTaskSet.Add(new QuestFlagTaskProgress
                    {
                        Number = flag.Number,
                        Info = flag
                    });
                }
            }
        }

        public void Init(PlayerObject player)
        {
            Owner = player;

            if (StartDateTime == DateTime.MinValue)
            {
                StartDateTime = Env.Now;
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);

            writer.Write(StartDateTime.ToBinary());
            writer.Write(EndDateTime.ToBinary());

            writer.Write(KillTaskCount.Count);
            foreach (var killTask in KillTaskCount)
            {
                writer.Write(killTask.MonsterID);
                writer.Write(killTask.Count);
            }

            writer.Write(ItemTaskCount.Count);
            foreach (var itemTask in ItemTaskCount)
            {
                writer.Write(itemTask.ItemID);
                writer.Write(itemTask.Count);
            }

            writer.Write(FlagTaskSet.Count);
            foreach (var flagTask in FlagTaskSet)
            {
                writer.Write(flagTask.Number);
                writer.Write(flagTask.State);
            }
        }


        public bool CheckCompleted()
        {
            UpdateTasks();

            bool canComplete = KillTaskCount.All(task => task.Complete);
            canComplete &= ItemTaskCount.All(task => task.Complete);
            canComplete &= FlagTaskSet.All(task => task.Complete);

            if (!canComplete) return false;

            if (!Completed)
            {
                EndDateTime = Env.Now;
                if (Info == null || Owner == null) throw new InvalidOperationException("Quest Info or Owner is null when trying to complete quest.");

                if (Info.TimeLimitInSeconds > 0)
                {
                    Owner.ExpireTimer($"Quest-{Index}");
                }
            }
// updatetask again to show gototask
            UpdateTasks();
            return true;
        }

        #region Need Requirement

        public bool NeedItem(ItemInfo iInfo)
        {
            return ItemTaskCount.Where((task, i) => task.Info.Item == iInfo && !task.Complete).Any();
        }

        public bool NeedKill(MonsterInfo mInfo)
        {
            return KillTaskCount.Where((task, i) => mInfo.Name.StartsWith(task.Info.Monster.Name, StringComparison.OrdinalIgnoreCase) && !task.Complete).Any();
        }

        public bool NeedFlag(int flagNumber)
        {
            return FlagTaskSet.Where((task, i) => task.Number == flagNumber && !task.Complete).Any();
        }

        #endregion

        #region Process Quest Task

        public void ProcessKill(MonsterInfo mInfo)
        {
            if (Info!.KillTasks.Count < 1) return;

            foreach (var taks in KillTaskCount.Where(taks => mInfo.Name.StartsWith(taks.Info.Monster.Name, StringComparison.OrdinalIgnoreCase)))
            {
                taks.Count++;
                return;
            }
        }

        public void ProcessItem(UserItem?[] inventory)
        {
            foreach (var task in ItemTaskCount)
            {
                var count = inventory.Where(item => item != null).
                    Where(item => item.Info.Name == task.Info.Item.Name).
                    Aggregate<UserItem, int>(0, (current, item) => current + item.Count);

                task.Count = count;
            }
        }

        public void ProcessFlag(bool[] Flags)
        {
            foreach (var task in FlagTaskSet)
            {
                for (int j = 0; j < Flags.Length - 1000; j++)
                {
                    if (task.Number != j || !Flags[j]) continue;

                    task.State = Flags[j];
                    break;
                }
            }
        }

        #endregion

        #region Update Task Messages

        public void UpdateTasks()
        {
            TaskList = [];

            UpdateKillTasks();
            UpdateItemTasks();
            UpdateFlagTasks();
            UpdateGotoTask();
        }

        public void UpdateKillTasks()
        {
            if (Info ==  null) return;
            
            if(Info.KillMessage.Length > 0 && Info.KillTasks.Count > 0) 
            {
                bool allComplete = KillTaskCount.All(task => task.Complete);

                TaskList.Add($"{Info.KillMessage} {(allComplete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")}");
                return;
            }

            for (int i = 0; i < KillTaskCount.Count; i++)
            {
                if (string.IsNullOrEmpty(Info.KillTasks[i].Message))
                {
                    TaskList.Add(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.KillCountProgress), KillTaskCount[i].Info.Monster.GameName, KillTaskCount[i].Count,
                        KillTaskCount[i].Info.Count, KillTaskCount[i].Complete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : ""));
                }
                else
                {
                    TaskList.Add($"{Info.KillTasks[i].Message} {(KillTaskCount[i].Complete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")}");
                }
            }
        }

        public void UpdateItemTasks()
        {
            if (Info ==  null) return;
            
            if (Info.ItemMessage.Length > 0 && Info.ItemTasks.Count > 0)
            {
                bool allComplete = ItemTaskCount.All(task => task.Complete);
                TaskList.Add($"{Info.ItemMessage} {(allComplete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")}");
                return;
            }

            for (int i = 0; i < ItemTaskCount.Count; i++)
            {
                if (string.IsNullOrEmpty(Info.ItemTasks[i].Message))
                {
                    TaskList.Add(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CollectProgress), Info.ItemTasks[i].Item.FriendlyName, ItemTaskCount[i].Count,
                        Info.ItemTasks[i].Count, ItemTaskCount[i].Complete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : ""));
                }
                else
                {
                    TaskList.Add($"{Info.ItemTasks[i].Message} {(ItemTaskCount[i].Complete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")}");
                }
            }
        }

        public void UpdateFlagTasks()
        {
            if (Info ==  null) return;
            
            if (Info.FlagMessage.Length > 0)
            {
                bool allComplete = FlagTaskSet.All(task => task.Complete);
                
                TaskList.Add($"{Info.FlagMessage} {(allComplete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")}");
                return;
            }

            for (int i = 0; i < FlagTaskSet.Count; i++)
            {
                TaskList.Add(string.IsNullOrEmpty(Info.FlagTasks[i].Message)
                    ? GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.ActivateFlag), Info.FlagTasks[i].Number,
                        FlagTaskSet[i].Complete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")
                    : $"{Info.FlagTasks[i].Message} {(FlagTaskSet[i].Complete ? GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TaskCompleted) : "")}");
            }
        }

        public void UpdateGotoTask()
        {
            if (Info ==  null) return;
            
            if (Info.GotoMessage.Length <= 0 || !Completed) return;
            
            TaskList.Add(Info.GotoMessage);
        }

        #endregion

        #region Optional Functions

        public void SetTimer()
        {
            if (Owner == null || Info == null)
            {
                return;
            }

            if (Info.TimeLimitInSeconds > 0)
            {
                var secondsSinceStarted = (int)(Env.Now - StartDateTime).TotalSeconds;

                var remainingSeconds = Info.TimeLimitInSeconds - secondsSinceStarted;

                if (remainingSeconds > 0)
                {
                    Owner.SetTimer($"Quest-{Index}", remainingSeconds, 1);
                }

                DelayedAction action = new DelayedAction(DelayedType.Quest, Env.Time + (remainingSeconds * 1000), this, QuestAction.TimeExpired, true);
                Owner.ActionList.Add(action);
            }
        }

        public void RemoveTimer()
        {
            if (Owner == null || Info == null)
            {
                return;
            }

            if (Info.TimeLimitInSeconds > 0)
            {
                Owner.ExpireTimer($"Quest-{Index}");
            }
        }

        #endregion

        public ClientQuestProgress CreateClientQuestProgress()
        {
            return new ClientQuestProgress
            {
                Id = Index,
                TaskList = TaskList,
                Taken = Taken,
                Completed = Completed,
                New = New
            };
        }
    }

    public class QuestKillTaskProgress
    {
        public int MonsterID { get; init; }
        public int Count { get; set; }
        public QuestKillTask? Info { get; set; }

        public bool Complete => Info != null && Count >= Info.Count;
    }

    public class QuestItemTaskProgress
    {
        public int ItemID { get; init; }
        public int Count { get; set; }
        public QuestItemTask? Info { get; set; }

        public bool Complete => Info != null && Count >= Info.Count;
    }

    public class QuestFlagTaskProgress
    {
        public int Number { get; init; }
        public bool State { get; set; }
        public QuestFlagTask? Info { get; set; }

        public bool Complete => Info != null && State == true;
    }
}
