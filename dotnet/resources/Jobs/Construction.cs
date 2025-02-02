﻿using GTANetworkAPI;
using System.Collections.Generic;
using System;
using UnitedRP.GUI;
using UnitedRP.Core;
using UnitedRP.Settings;
using UnitedRP.Utils;

namespace UnitedRP.Jobs
{
    class Construction : Script
    {
        private static int JobMultiper = 3;
        private static int checkpointPayment = 10;
        private static int JobWorkId = 11;
        private static int JobsMinLVL = 1;
        private static nLog Log = new nLog("L");

        [ServerEvent(Event.ResourceStart)]
        public void Event_ResourceStart()
        {
            try
            {
                ColShape col;

                int i = 0;
                foreach (var Check in Checkpoints)
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER2", i);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    i++;
                };

                i = 0;
                foreach (var Check in Checkpoints2)
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 4, 2, 0);
                    col.SetData("NUMBER3", i);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    i++;
                };
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        #region Чекпоинты
        private static List<Checkpoint> Checkpoints = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(22.8898, -402.6957, 44.43897), 19.49689), // Тоскает коробку
            new Checkpoint(new Vector3(28.69614, -452.2071, 44.43775), 243.5095), // Тоскает мусор цемент
            new Checkpoint(new Vector3(33.00998, -444.1138, 44.43775), 207.824), // Тоскает кирпич
            new Checkpoint(new Vector3(33.96332, -450.0271, 44.43776), 215.6413), // Тоскает мусор
            new Checkpoint(new Vector3(39.33658, -430.9125, 44.43895), 72.72858), // Электрик
            new Checkpoint(new Vector3(38.71178, -401.6262, 44.43897), 68.8477), // Стучит молотком
            new Checkpoint(new Vector3(38.71178, -401.6262, 44.43897), 341.7192), // Стучит молотком
        };
        private static List<Checkpoint> Checkpoints2 = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(54.49169, -377.0713, 44.43923), 152.6219), // Тоскает коробку Конец
            new Checkpoint(new Vector3(60.10947, -398.5452, 38.80103), 65.90971), // Тоскает мусор Конец
            new Checkpoint(new Vector3(46.74149, -391.0762, 44.43874), 253.3996), // Тоскает кирпич Конец
            new Checkpoint(new Vector3(60.10947, -398.5452, 38.80103), 65.90971), // Тоскает мусор Конец
        };
        #endregion
		
		

        public static void StartWorkDay(Player player)
        {
            if (Main.Players[player].LVL < JobsMinLVL)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Необходим как минимум {JobsMinLVL} уровень", 3000);
                return;
            }
            if (Main.Players[player].WorkID != 11)
            {
                Jobs.WorkManager.Layoff(player);
            }


            Main.Players[player].WorkID = 11;
            if (player.GetData<bool>("ON_WORK"))
            {
                if (player.GetData<int>("PACKAGES") == 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Сдайте коробку прежде чем закончить рабочий день", 3000);
                    return;
                
                }
                Customization.ApplyCharacter(player);
                player.SetData("PACKAGES", 0);
                player.SetData("ON_WORK", false);
                Trigger.PlayerEvent(player, "deleteCheckpoint", 15);
                Trigger.PlayerEvent(player, "deleteWorkBlip");

                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы получили зарплату в размере: {player.GetData<int>("PAYMENT")}$", 3000);

                MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));

                QuestsManager.AddQuestProcess(player, 11, player.GetData<int>("PAYMENT"));
                UnitedRP.Families.Family.GiveMoneyOnJob(player, player.GetData<int>("PAYMENT"));
                Trigger.PlayerEvent(player, "CloseJobStatsInfo", player.GetData<int>("PAYMENT"));
                player.SetData("PAYMENT", 0);



                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы закончили рабочий день!", 3000);
                return;
            }
            else
            {
                Customization.ClearClothes(player, Main.Players[player].Gender);
                if (Main.Players[player].Gender)
                {
                    player.SetAccessories(0, 145, 0);
                    player.SetClothes(3, 30, 0);
                    player.SetClothes(8, 59, 0);
                    player.SetClothes(11, 1, 0);
                    player.SetClothes(4, 0, 5);
                    player.SetClothes(6, 48, 0);
                }
                else
                {
                    player.SetAccessories(0, 144, 0);
                    player.SetClothes(3, 11, 0);
                    player.SetClothes(8, 36, 0);
                    player.SetClothes(11, 0, 0);
                    player.SetClothes(4, 1, 5);
                    player.SetClothes(6, 49, 0);
                }

                var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                player.SetData("WORKCHECK", check);
                Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position - new Vector3(0, 0, 2.5f), 3, 0, 255, 255, 255);
                Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints[check].Position);
                player.SetData("PACKAGES", 0);

                player.SetData("PAYMENT", 0);
                Trigger.PlayerEvent(player, "JobStatsInfo", player.GetData<int>("PAYMENT"));

                player.SetData("ON_WORK", true);
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Вы начали работать строителем!", 3000);
                GUI.Dashboard.sendStats(player);
                return;
            }
        }

        #region Когда заходишь в чекпоинт
        private static void PlayerEnterCheckpoint(ColShape shape, Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (Main.Players[player].WorkID != 11 || !player.GetData<bool>("ON_WORK")) return;

                if (player.GetData<int>("PACKAGES") == 0)
                {
                    if (shape.GetData<int>("NUMBER2") == player.GetData<int>("WORKCHECK"))
                    {
                        player.SetData("PACKAGES", player.GetData<int>("PACKAGES") + 1);
                        player.SetData("WORKCHECK", -1);

                        NAPI.Entity.SetEntityPosition(player, Checkpoints[shape.GetData<int>("NUMBER2")].Position + new Vector3(0, 0, 1.2));
                        NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, Checkpoints[shape.GetData<int>("NUMBER2")].Heading));
                        if (shape.GetData<int>("NUMBER2") < 4)
                        {
                            #region Таскает коробку 1
                            if (shape.GetData<int>("NUMBER2") == 0)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 47);

                                var check = shape.GetData<int>("NUMBER2");
                                player.SetData("WORKCHECK", check);
                                Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints2[check].Position, 5, 0, 255, 0, 0);
                                Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints2[check].Position);

                                NAPI.Task.Run(() =>
                                {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            player.PlayAnimation("anim@heists@box_carry@", "idle", 49);
                                            BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("ng_proc_box_01a"), 18905, new Vector3(0.1, 0.1, 0.3), new Vector3(-10, -75, -40));
                                        }
                                    }
                                    catch { }
                                }, 250);
                            }
                            #endregion
                            #region Таскает мусор цемент
                            if (shape.GetData<int>("NUMBER2") == 1)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("amb@prop_human_bum_bin@idle_a", "idle_a", 47);

                                var check = shape.GetData<int>("NUMBER2");
                                player.SetData("WORKCHECK", check);
                                Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints2[check].Position, 5, 0, 255, 0, 0);
                                Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints2[check].Position);

                                NAPI.Task.Run(() =>
                                {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            player.PlayAnimation("amb@world_human_janitor@male@base", "base", 49);
                                            BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_cs_rub_binbag_01"), 57005, new Vector3(0.1, 0.0, 0.0), new Vector3(-90, 0, 0));
                                        }
                                    }
                                    catch { }
                                }, 250);
                            }
                            #endregion
                            #region Таскает кирпичи 2
                            if (shape.GetData<int>("NUMBER2") == 2)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 47);

                                var check = shape.GetData<int>("NUMBER2");
                                player.SetData("WORKCHECK", check);
                                Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints2[check].Position, 5, 0, 255, 0, 0);
                                Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints2[check].Position);

                                NAPI.Task.Run(() =>
                                {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            player.StopAnimation();
                                            Main.OffAntiAnim(player);
                                            BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("ng_proc_brick_01a"), 57005, new Vector3(0.1, 0.0, 0.0), new Vector3(-90, 0, 0));
                                        }
                                    }
                                    catch { }
                                }, 250);
                            }
                            #endregion
                            #region Таскает мусор
                            if (shape.GetData<int>("NUMBER2") == 3)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("amb@prop_human_bum_bin@idle_a", "idle_a", 47);

                                var check = shape.GetData<int>("NUMBER2");
                                player.SetData("WORKCHECK", check);
                                Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints2[check].Position, 5, 0, 255, 0, 0);
                                Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints2[check].Position);

                                NAPI.Task.Run(() =>
                                {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            player.PlayAnimation("amb@world_human_janitor@male@base", "base", 49);
                                            BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_cs_rub_binbag_01"), 57005, new Vector3(0.1, 0.0, 0.0), new Vector3(-90, 0, 0));
                                        }
                                    }
                                    catch { }
                                }, 4000);
                            }
                            #endregion
                        }
                        else
                        {
                            #region Электрик
                            if (shape.GetData<int>("NUMBER2") == 4)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("amb@prop_human_movie_studio_light@base", "base", 39);
                                Trigger.PlayerEvent(player, "deleteCheckpoint", 15);
                                Trigger.PlayerEvent(player, "deleteWorkBlip");

                                NAPI.Task.Run(() => {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            int costformash = (int)Math.Floor((checkpointPayment + 6 * Main.GetJobPoints(player)) * JobMultiper * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                                            player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + costformash);
                                            Trigger.PlayerEvent(player, "JobStatsInfo", player.GetData<int>("PAYMENT"));


                                            player.SetData("PACKAGES", player.GetData<int>("PACKAGES") - 1);

                                            player.StopAnimation();
                                            Main.OffAntiAnim(player);
                                            var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                                            player.SetData("WORKCHECK", check);
                                            Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position, 2, 0, 255, 0, 0);
                                            Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints[check].Position);
                                        }
                                    }
                                    catch { }
                                }, 4000);
                            }
                            #endregion
                            #region Стучит молотком 1
                            if (shape.GetData<int>("NUMBER2") == 5)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("amb@world_human_hammering@male@base", "base", 39);
                                BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_tool_hammer"), 57005, new Vector3(0.1, 0.0, 0.0), new Vector3(-90, 0, 0));
                                Trigger.PlayerEvent(player, "deleteCheckpoint", 15);
                                Trigger.PlayerEvent(player, "deleteWorkBlip");

                                NAPI.Task.Run(() => {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            int costformash = (int)Math.Floor((checkpointPayment + 6 * Main.GetJobPoints(player)) * JobMultiper * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                                            player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + costformash);
                                            Trigger.PlayerEvent(player, "JobStatsInfo", player.GetData<int>("PAYMENT"));

                                            player.SetData("PACKAGES", player.GetData<int>("PACKAGES") - 1);

                                            BasicSync.DetachObject(player);
                                            player.StopAnimation();
                                            Main.OffAntiAnim(player);
                                            var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                                            player.SetData("WORKCHECK", check);
                                            Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position, 2, 0, 255, 0, 0);
                                            Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints[check].Position);
                                        }
                                    }
                                    catch { }
                                }, 4000);
                            }
                            #endregion
                            #region Стучит молотком 2
                            if (shape.GetData<int>("NUMBER2") == 6)
                            {
                                Main.OnAntiAnim(player);
                                player.PlayAnimation("amb@world_human_hammering@male@base", "base", 39);
                                BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_tool_hammer"), 57005, new Vector3(0.1, 0.0, 0.0), new Vector3(-90, 0, 0));
                                Trigger.PlayerEvent(player, "deleteCheckpoint", 15);
                                Trigger.PlayerEvent(player, "deleteWorkBlip");

                                NAPI.Task.Run(() => {
                                    try
                                    {
                                        if (player != null && Main.Players.ContainsKey(player))
                                        {
                                            int costformash = (int)Math.Floor((checkpointPayment + 6 * Main.GetJobPoints(player)) * JobMultiper * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                                            player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + costformash);
                                            Trigger.PlayerEvent(player, "JobStatsInfo", player.GetData<int>("PAYMENT"));

                                            player.SetData("PACKAGES", player.GetData<int>("PACKAGES") - 1);

                                            BasicSync.DetachObject(player);
                                            player.StopAnimation();
                                            Main.OffAntiAnim(player);
                                            var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                                            player.SetData("WORKCHECK", check);
                                            Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position, 2, 0, 255, 0, 0);
                                            Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints[check].Position);
                                        }
                                    }
                                    catch { }
                                }, 4000);
                            }
                            #endregion

                        }

                    }
                }
                else
                {
                    if (shape.GetData<int>("NUMBER3") == player.GetData<int>("WORKCHECK"))
                    {
                        player.SetData("PACKAGES", player.GetData<int>("PACKAGES") - 1);
                        player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 47);

                        player.SetData("WORKCHECK", -1);
                        var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                        player.SetData("WORKCHECK", check);
                        Trigger.PlayerEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position, 2, 0, 255, 0, 0);
                        Trigger.PlayerEvent(player, "createWorkBlip", Checkpoints[check].Position);
                        NAPI.Task.Run(() =>
                        {
                            try
                            {
                                if (player != null && Main.Players.ContainsKey(player))
                                {
                                    int costformash = (int)Math.Floor((checkpointPayment + 5 * Main.GetJobPoints(player)) * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                                    int level = Main.Players[player].LVL > 5 ? 12 : 2 * Main.Players[player].LVL;
                                    player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + costformash + level);

                                    player.SendNotification($"Строитель: ~h~~g~+{costformash + level}$", true);

                                    Trigger.PlayerEvent(player, "JobStatsInfo", player.GetData<int>("PAYMENT"));

                                    BasicSync.DetachObject(player);
                                    player.StopAnimation();
                                    Main.OffAntiAnim(player);
                                }
                            }
                            catch { }
                        }, 400);
                    }
                }

            }
            catch (Exception e) { Log.Write("PlayerEnterCheckpoint: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        #region Если игрок умер
        public static void Event_PlayerDeath(Player player, Player entityKiller, uint weapon)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (Main.Players[player].WorkID == JobWorkId && player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    Trigger.PlayerEvent(player, "deleteCheckpoint", 15);
                    Trigger.PlayerEvent(player, "deleteWorkBlip");
                    player.SetData("PACKAGES", 0);


                    MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));
                    Trigger.PlayerEvent(player, "CloseJobStatsInfo", player.GetData<int>("PAYMENT"));
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы получили зарплату в размере: {player.GetData<int>("PAYMENT")}$", 3000);
                    player.SetData("PAYMENT", 0);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);

                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"+ {player.GetData<int>("PAYMENT")}$", 3000);
                }
            }
            catch (Exception e) { Log.Write("PlayerDeath: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        #region Если игрок вышел из игры или его кикнуло
        public static void Event_PlayerDisconnected(Player player, DisconnectionType type, string reason)
        {
            try
            {
                if (Main.Players[player].WorkID == JobWorkId && player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    Trigger.PlayerEvent(player, "deleteCheckpoint", 15);
                    Trigger.PlayerEvent(player, "deleteWorkBlip");
                    player.SetData("PACKAGES", 0);

                    MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));
                    Trigger.PlayerEvent(player, "CloseJobStatsInfo", player.GetData<int>("PAYMENT"));
                    player.SetData("PAYMENT", 0);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);

                }
            }
            catch (Exception e) { Log.Write("PlayerDisconnected: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        internal class Checkpoint
        {
            public Vector3 Position { get; }
            public double Heading { get; }

            public Checkpoint(Vector3 pos, double rot)
            {
                Position = pos;
                Heading = rot;
            }
        }
    }
}
