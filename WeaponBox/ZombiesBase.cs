using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using InfinityScript;

namespace Zombies
{
    public class Zombies : BaseScript
    {
        private Entity _airdropCollision;
        private Random _rng = new Random();
        private string _mapname;
        private int MoneyFX;
        //private int RaygunShot;
        //private int RaygunShotU;
        //private int RaygunImpactFX;
        //private int RaygunImpactFXU;
        public int RoundNo;
        private int ZombieHealth;
        private int CrateFX;
        private int RockFX;
        public string[] PerkDescs = { "More health", "Sprint faster and forever", "Reload faster", "An extra weapon slot", "Faster rate of fire", "Faster moving while ADS", "Scavenge ammo from the dead", "Jump to dodge zombie attacks" };

        List<Entity> ZombiesDead = new List<Entity>();
        List<Entity> TotalZombies = new List<Entity>();

        public Zombies()
            : base()
        {
            CrateFX = Call<int>("loadfx", "smoke/thin_black_smoke_s_fast");
            RockFX = Call<int>("loadfx", "smoke/battlefield_smokebank_S_warm_dense");
            Entity care_package = Call<Entity>("getent", "care_package", "targetname");
            _airdropCollision = Call<Entity>("getent", care_package.GetField<string>("target"), "targetname");
            _mapname = Call<string>("getdvar", "mapname");
            //Call("precachemodel", getAlliesFlagModel(_mapname));
            //Call("precachemodel", getAxisFlagModel(_mapname));
            //Call("precachemodel", "prop_flag_neutral");
            Call("precacheshader", "waypoint_flag_friendly");
            Call("precacheshader", "compass_waypoint_target");
            Call("precacheshader", "compass_waypoint_bomb");
            Call("precacheShader", "specialty_stalker");
            Call("precacheShader", "specialty_longersprint");
            Call("precacheShader", "specialty_fastreload");
            Call("precacheShader", "specialty_twoprimaries");
            Call("precacheShader", "cardicon_juggernaut_1");
            Call("precacheShader", "cardicon_gears");
            Call("precacheShader", "cardicon_league_1911");
            Call("precacheShader", "cardicon_brassknuckles");
            Call("precacheShader", "weapon_claymore");
            Call("precacheShader", "weapon_colt_45");
            //Call("precachemodel", "weapon_scavenger_grenadebag");
            Call(341, "allies", true);
            MoneyFX = Call<int>("loadfx", "props/cash_player_drop");
            //RaygunShot = Call<int>("loadfx", "misc/aircraft_light_wingtip_green");
            //RaygunShotU = Call<int>("loadfx", "misc/aircraft_light_wingtip_red");
            //RaygunImpactFX = Call<int>("loadfx", "misc/flare_ambient_green");
            //RaygunImpactFXU = Call<int>("loadfx", "misc/flare_ambient");
            ZombieHealth = 200;
            RoundNo = 1;
            //Call(42, "g_hardcore", "1");

            if (File.Exists("scripts\\maps\\" + _mapname + "_zm.txt"))
                loadMapEdit(_mapname);

            OnNotify("zombiesDied", () =>
            {
                AfterDelay(19000, () =>
                {
                    //player.Call("iprintlnbold", "Round " + RoundNo + " started!");//Removing this to make the mode more like BO. Round fade is good
                    Notify("RoundStart");
                });
                foreach (Entity player in Players)
                {
                    if (!player.IsPlayer) continue;
                    if (player.GetField<string>("sessionteam") == "allies")
                    {
                        player.Call("iprintlnbold", "Round " + RoundNo + " completed!");
                        player.SetField("cash", player.GetField<int>("cash") + 250);
                        //Notify("scorePopup", 250, "G", player.EntRef);
                        ScorePopup(player, 250, "G", player.EntRef);
                        AfterDelay(3500, () =>
                                player.Call("iprintlnbold", "You have 15 seconds before the next round starts."));
                    }
                    //else
                    //{
                        AfterDelay(19000, () =>
                        {
                            HudElem counter = player.GetField<HudElem>("hud_roundCounter");
                            player.SetField("RoundNo", player.GetField<int>("RoundNo") + 1);
                            FadeRound(counter, player);
                        });
                    //}
                }
            });
            initPrisonBreak();
            PlayerConnected += new Action<Entity>(player =>
            {
                player.SetClientDvar("g_teamname_allies", "^2Humans");
                player.SetClientDvar("g_teamname_axis", "^1Zombies");
                player.SetClientDvar("cg_objectiveText", "Survive the attacks of the undead.");
                player.SetClientDvar("g_hardcore", "1");
                player.SetField("NewGunReady", 1); // feature to give 2 guns or a fix
                player.SetField("perk1bought", 0); // set perks to not used for buying
                player.SetField("perk2bought", 0);
                player.SetField("perk3bought", 0);
                player.SetField("perk4bought", 0);
                player.SetField("perk5bought", 0);
                player.SetField("perk6bought", 0);
                player.SetField("perk7bought", 0);
                player.SetField("perk8bought", 0);
                player.SetField("juggHUDDone", 0);
                player.SetField("staminaHUDDone", 0);
                player.SetField("speedHUDDone", 0);
                player.SetField("mulekickHUDDone", 0);
                player.SetField("dtapHUDDone", 0);
                player.SetField("stalkerHUDDone", 0);
                player.SetField("PERK7HUDDone", 0);
                player.SetField("PERK8HUDDone", 0);
                player.SetField("PerkBought", "");
                player.SetField("GamblerInUse", 0);
                player.SetField("GamblerReady", 1);
                player.SetField("hasHealthHud", 0);
                player.SetField("maxhealth", 200);
                for (int i = 0; i < 10; i++)
                    player.SetField("ScoreFadeout" + i.ToString(), 0);
                player.SetField("Lives", 10);
                player.SetField("RoundNo", RoundNo);

                // usable notifications
                player.Call("notifyonplayercommand", "triggeruse", "+activate");
                player.OnNotify("triggeruse", (ent) => HandleUseables(ent));

                //player.Call("notifyonplayercommand", "fly", "+frag");

                UsablesHud(player);
                player.SetField("cash", 500);
                createPlayerHud(player);

                player.OnNotify("weapon_change", (ent, newWeap) =>
                {
                    UpdateHUDAmmo(ent);
                });

                player.OnNotify("weapon_fired", (ent, weapon) =>
                {
                    UpdateHUDAmmo(ent);
                });

                player.OnNotify("reload", (ent) =>
                {
                    UpdateHUDAmmo(ent);
                });

                HandleUpgradeSpecialWeps(player);
                /*
                player.OnNotify("fly", (ent) =>
                {
                    if (player.GetField<string>("sessionstate") != "spectator")
                    {
                        player.Call("allowspectateteam", "freelook", true);
                        player.SetField("sessionstate", "spectator");
                        player.Call("setcontents", 0);
                    }
                    else
                    {
                        player.Call("allowspectateteam", "freelook", false);
                        player.SetField("sessionstate", "playing");
                        player.Call("setcontents", 100);
                    }
                });
                */
                player.Call("givemaxammo", "iw5_usp45_mp");
                updatePlayerScores(player.EntRef + 1);

                player.SpawnedPlayer += new Action(() =>
                {
                    player.SetClientDvar("g_hardcore", "1");
                    player.SetClientDvar("cg_objectiveText", "Survive the attacks of the undead.");
                    if (player.GetField<string>("sessionteam") == "axis")
                    {
                        ZombieChecker(player);
                        //int newHealth = player.GetField<int>("maxhealth") + 5;
                        //player.SetField("maxhealth", newHealth);
                        //player.Health = newHealth;
                        if (!player.HasField("bohud_created")) return;
                        if (player.GetField<int>("hasHealthHud") == 0) LifeHandler(player);
                        //HudElem healthH = HudElem.CreateFontString(player, "hudbig", 0.9f);
                        //healthH.SetPoint("TOP LEFT", "TOP LEFT", 5, 25);
                        //healthH.HideWhenInMenu = true;
                        //healthH.Alpha = 0;
                        /*
                        if (player.GetField<int>("hasHealthHud") == 0)
                        {
                            FadeIn(healthH);
                            OnInterval(100, () =>
                            {
                                if (player.IsAlive)
                                {
                                    healthH.SetText("Health: " + player.Health.ToString());
                                    return true;
                                }
                                else
                                {
                                    FadeOut(healthH);
                                    AfterDelay(1000, () =>
                                        healthH.Call("destroy"));
                                    player.SetField("hasHealthHud", 0);
                                    return false;
                                }
                            });
                            player.SetField("hasHealthHud", 1);
                        }
                         */
                        destroyPointHUDs(player);
                    }
                    /*
                    foreach (Entity players in Players)
                    {
                        if (player.GetField<string>("sessionteam") == "allies")
                        {
                            if (!players.HasField("PointHUDDestroyed"))
                            {
                                Console.Write("Faded in HUD" + player.EntRef + 1);
                                updatePlayerScores(players, player.EntRef + 1);
                            }
                        }
                    }
                     */
                });
            });
        }

        public override void OnPlayerDisconnect(Entity player)
        {
            if (TotalZombies.Contains(player))
            {
                TotalZombies.Remove(player);
            }
            if (ZombiesDead.Contains(player))
            {
                ZombiesDead.Remove(player);
            }
                foreach (Entity players in Players)
                {
                    if (!players.HasField("PointHUDDestroyed") && players.IsAlive)
                    {
                        players.SetField("ScoreFadeout" + player.EntRef, 0);
                        var scoreHUDs = players.GetField<HudElem[]>("playerScoreHUDs");
                        FadeOut(scoreHUDs[player.EntRef]);
                    }
                }
        }

        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            AfterDelay(100, () =>
                {
                    if (attacker.IsAlive && player.GetField<string>("sessionteam") != "allies")
                    {
                        attacker.SetField("cash", attacker.GetField<int>("cash") + 100);
                        foreach (Entity players in Players)
                            ScorePopup(players, 100, "G", attacker.EntRef);
                        player.SetField("lives", player.GetField<int>("lives") - 1);
                    }
                    if (attacker.GetField<string>("sessionteam") == "axis")
                    {
                        if (player.HasField("bohud_created"))
                        {
                            player.AfterDelay(500, (p) =>
                                {
                                    destroyPointHUDs(p);
                                    destroyPerkHUD(p);
                                });
                        }
                    }
                    if (attacker.GetField<string>("sessionteam") == "allies" && !player.HasField("PointHUDDestroyed"))
                    {
                        AfterDelay(500, () =>
                            {
                                destroyPointHUDs(player);
                                updatePlayerScores(0);
                            });
                    }
                });
        }

        public override void OnPlayerDamage(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            AfterDelay(100, () =>
                {
                    if (attacker == null || player == null || inflictor == null) return;
                    if (attacker.IsAlive && player.GetField<string>("sessionteam") != "allies")
                    {
                        attacker.SetField("cash", attacker.GetField<int>("cash") + 10);
                        //updatePlayerScores(0);
                        foreach (Entity players in Players)
                            ScorePopup(players, 10, "G", attacker.EntRef);
                        if (attacker.GetField<string>("sessionteam") == "allies" && ZombiesDead.Contains(player) && (mod != "MOD_EXPLOSIVE" || weapon != "nuke_mp"))//Fix aimbotters killing jailed zombies
                            attacker.Call(33340, inflictor, attacker, damage, dFlags, mod, weapon, attacker.Origin, dir, hitLoc, 0, 0);
                        if (attacker.GetField<string>("sessionteam") != "allies") return;
                        int Lives = player.GetField<int>("Lives");
                        int Health = player.Health;
                        HudElem livetext = player.GetField<HudElem>("hud_lives");
                        HudElem healthtext = player.GetField<HudElem>("hud_health");
                        //livetext.SetText("Lives: " + Lives);
                        //health.SetText("Health: " + Health);
                        livetext.Children[0].Call(32963, Lives);
                        healthtext.Children[0].Call(32963, Health);
                    }
                });
        }

        public override void OnSay(Entity player, string name, string message)
        {
            if (message.StartsWith("viewpos") && player.GetField<string>("name") == "Slvr99")
            {
                print("({0}, {1}, {2})", player.Origin.X, player.Origin.Y, player.Origin.Z);
            }
            if (message.StartsWith("givepoints") && player.GetField<string>("name") == "Slvr99")
            {
                player.SetField("cash", player.GetField<int>("cash") + 10000);
                //updatePlayerScores(0);
                foreach (Entity players in Players)
                ScorePopup(players, 10000, "G", player.EntRef);
            }
        }

        public void CreateRamp(Vector3 top, Vector3 bottom)
        {
            float distance = top.DistanceTo(bottom);
            int blocks = (int)Math.Ceiling(distance / 30);
            Vector3 A = new Vector3((top.X - bottom.X) / blocks, (top.Y - bottom.Y) / blocks, (top.Z - bottom.Z) / blocks);
            Vector3 temp = Call<Vector3>("vectortoangles", new Parameter(top - bottom));
            Vector3 BA = new Vector3(temp.Z, temp.Y + 90, temp.X);
            for (int b = 0; b <= blocks; b++)
            {
                spawnCrate(bottom + (A * b), BA, false, false);
            }
        }

        public static List<Entity> usables = new List<Entity>();
        public void HandleUseables(Entity player)
        {
            foreach (Entity ent in usables)
            {
                if (player.Origin.DistanceTo(ent.Origin) < ent.GetField<int>("range"))
                {
                    string UseType = ent.GetField<string>("usabletype");
                    switch (UseType)
                    {
                        case "door":
                            usedDoor(ent, player);
                            break;
                        case "randombox":
                            usedBox(ent, player);
                            break;
                        case "pap":
                            usedPapBox(ent, player);
                            break;
                        case "upgradebox":
                            usedUpgradeBox(ent, player);
                            break;
                        case "gambler":
                            usedGambler(ent, player);
                            break;
                        case "perk1":
                            usedPerk1(ent, player);
                            break;
                        case "perk2":
                            usedPerk2(ent, player);
                            break;
                        case "perk3":
                            usedPerk3(ent, player);
                            break;
                        case "perk4":
                            usedPerk4(ent, player);
                            break;
                        case "perk5":
                            usedPerk5(ent, player);
                            break;
                        case "perk6":
                            usedPerk6(ent, player);
                            break;
                        case "perk7":
                            usedPerk7(ent, player);
                            break;
                        case "perk8":
                            usedPerk8(ent, player);
                            break;
                        case "claymore":
                            usedClaymore(ent, player);
                            break;
                        case "wallweapon":
                            usedWallWeapon(ent, player);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public static void runOnUsable(Func<Entity, bool> func, string type)
        {
            foreach (Entity ent in usables)
            {
                if (ent.GetField<string>("usabletype") == type)
                {
                    func.Invoke(ent);
                }
            }
        }

        public static void notifyUsables(string notify)
        {
            foreach (Entity usable in usables)
            {
                usable.Notify(notify);
            }
        }

        private void createPlayerHud(Entity player)
        {
            initRoundsLives(player);

            if (player.HasField("bohud_created"))
            {
                return;
            }

            // ammo stuff
            var ammoSlash = HudElem.CreateFontString(player, "hudsmall", 1f);
            ammoSlash.SetPoint("bottom right", "bottom right", -85, -35);
            ammoSlash.HideWhenInMenu = true;
            ammoSlash.Archived = false;
            ammoSlash.SetText("/");

            player.SetField("bohud_ammoSlash", new Parameter(ammoSlash));

            var ammoStock = HudElem.CreateFontString(player, "hudsmall", 1f);
            ammoStock.Parent = ammoSlash;
            ammoStock.SetPoint("bottom left", "bottom left", 3, 0);
            ammoStock.HideWhenInMenu = true;
            ammoStock.Archived = false;
            ammoStock.Call("setvalue", 48);

            player.SetField("bohud_ammoStock", new Parameter(ammoStock));

            var ammoClip = HudElem.CreateFontString(player, "hudbig", 1f);
            ammoClip.Parent = ammoSlash;
            ammoClip.SetPoint("right", "right", -7, -4);
            ammoClip.HideWhenInMenu = true;
            ammoClip.Archived = false;
            ammoClip.Call("setvalue", 12);

            var weaponName = HudElem.CreateFontString(player, "hudsmall", 1f);
            weaponName.SetPoint("bottom right", "bottom right", -64, -15);
            weaponName.HideWhenInMenu = true;
            weaponName.Archived = false;
            weaponName.SetText("");

            UpdateHUDAmmo(player);

            var player0PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player0PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 150);
            player0PointHUD.HideWhenInMenu = true;
            player0PointHUD.Alpha = 0;
            var player1PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player1PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 135);
            player1PointHUD.HideWhenInMenu = true;
            player1PointHUD.Alpha = 0;
            player1PointHUD.Color = new Vector3(0.1f, 1, 1);
            var player2PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player2PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 120);
            player2PointHUD.HideWhenInMenu = true;
            player2PointHUD.Color = new Vector3(1, 0.5f, 0);
            player2PointHUD.Alpha = 0;
            var player3PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player3PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 105);
            player3PointHUD.HideWhenInMenu = true;
            player3PointHUD.Color = new Vector3(0.1f, 1, 0.1f);
            player3PointHUD.Alpha = 0;
            var player4PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player4PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 90);
            player4PointHUD.HideWhenInMenu = true;
            player4PointHUD.Color = new Vector3(1, 0.1f, 0.1f);
            player4PointHUD.Alpha = 0;
            var player5PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player5PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 75);
            player5PointHUD.HideWhenInMenu = true;
            player5PointHUD.Color = new Vector3(0, 0, 0.8f);
            player5PointHUD.Alpha = 0;
            var player6PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player6PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 60);
            player6PointHUD.HideWhenInMenu = true;
            player6PointHUD.Color = new Vector3(0.7f, 0.7f, 0.7f);
            player6PointHUD.Alpha = 0;
            var player7PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player7PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 45);
            player7PointHUD.HideWhenInMenu = true;
            player7PointHUD.Color = new Vector3(0.8f, 0.8f, 0);
            player7PointHUD.Alpha = 0;
            var player8PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player8PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 30);
            player8PointHUD.HideWhenInMenu = true;
            player8PointHUD.Color = new Vector3(0.2f, 0.5f, 0.2f);
            player8PointHUD.Alpha = 0;
            var player9PointHUD = HudElem.CreateFontString(player, "normalFont", 1.5f);
            player9PointHUD.SetPoint("LEFTCENTER", "RIGHTCENTER", -90, 15);
            player9PointHUD.HideWhenInMenu = true;
            player9PointHUD.Color = new Vector3(0.6f, 0.2f, 1);
            player9PointHUD.Alpha = 0;

            HudElem[] playerScores = new HudElem[10] { player0PointHUD, player1PointHUD, player2PointHUD, player3PointHUD, player4PointHUD, player5PointHUD, player6PointHUD, player7PointHUD, player8PointHUD, player9PointHUD };
            playerScores[player.EntRef].FontScale = 1.8f;

            player.SetField("playerScoreHUDs", new Parameter(playerScores));

            player.SetField("bohud_weaponName", new Parameter(weaponName));

            player.SetField("bohud_ammoClip", new Parameter(ammoClip));
            
            player.SetField("bohud_ammoSlash", new Parameter(ammoSlash));

                HudElem jugg = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
                jugg.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 0, -5);
                jugg.HideWhenInMenu = true;
                jugg.Foreground = true;
                jugg.Alpha = 0;

            HudElem stamina = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            stamina.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 32, -5);
            stamina.HideWhenInMenu = true;
            stamina.Foreground = true;
            stamina.Alpha = 0;

            HudElem speed = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            speed.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 64, -5);
            speed.HideWhenInMenu = true;
            speed.Foreground = true;
            speed.Alpha = 0;

            HudElem mulekick = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            mulekick.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 96, -5);
            mulekick.HideWhenInMenu = true;
            mulekick.Foreground = true;
            mulekick.Alpha = 0;

            HudElem dtap = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            dtap.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 128, -5);
            dtap.HideWhenInMenu = true;
            dtap.Foreground = true;
            dtap.Alpha = 0;

            HudElem stalker = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            stalker.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 160, -5);
            stalker.HideWhenInMenu = true;
            stalker.Foreground = true;
            stalker.Alpha = 0;

            HudElem perk7 = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            perk7.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 192, -5);
            perk7.HideWhenInMenu = true;
            perk7.Foreground = true;
            perk7.Alpha = 0;

            HudElem perk8 = HudElem.CreateIcon(player, "specialty_placeholder", 30, 30);
            perk8.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 224, -5);
            perk8.HideWhenInMenu = true;
            perk8.Foreground = true;
            perk8.Alpha = 0;

            player.SetField("juggHUD", new Parameter(jugg));
            player.SetField("staminaHUD", new Parameter(stamina));
            player.SetField("speedHUD", new Parameter(speed));
            player.SetField("mulekickHUD", new Parameter(mulekick));
            player.SetField("dtapHUD", new Parameter(dtap));
            player.SetField("stalkerHUD", new Parameter(stalker));
            player.SetField("perk7HUD", new Parameter(perk7));
            player.SetField("perk8HUD", new Parameter(perk8));
            player.SetField("bohud_created", true);
            updatePlayerScores(player.EntRef + 1);
        }

        private void destroyPointHUDs(Entity player)
        {
            if (!player.HasField("bohud_created") || player.HasField("PointHUDDestroyed"))
                return;
            HudElem[] scoreHUDs = player.GetField<HudElem[]>("playerScoreHUDs");
            for (int i = 0; i < 10; i++)
                scoreHUDs[i].Call("destroy");
            player.SetField("PointHUDDestroyed", true);
        }

        private void destroyPerkHUD(Entity player)
        {
            if (!player.HasField("bohud_created"))
                return;
            var jugg = player.GetField<HudElem>("juggHUD");
            var stamina = player.GetField<HudElem>("staminaHUD");
            var speed = player.GetField<HudElem>("speedHUD");
            var mulekick = player.GetField<HudElem>("mulekickHUD");
            var dtap = player.GetField<HudElem>("dtapHUD");
            var stalker = player.GetField<HudElem>("stalkerHUD");
            var perk7 = player.GetField<HudElem>("perk7HUD");
            var perk8 = player.GetField<HudElem>("perk8HUD");
            jugg.Call("destroy");
            stamina.Call("destroy");
            speed.Call("destroy");
            mulekick.Call("destroy");
            dtap.Call("destroy");
            stalker.Call("destroy");
            perk7.Call("destroy");
            perk8.Call("destroy");
        }

        public void updatePlayerScores(int FadeInHUD)
        {
            foreach (Entity player in Players)
            {
                if (!player.IsAlive || !player.IsPlayer || !player.HasField("bohud_created") || player.EntRef > 9) continue;
                if (player.GetField<string>("sessionteam") == "axis") continue;
                Entity[] scoreList = new Entity[10];
                HudElem[] scoreHUDs = player.GetField<HudElem[]>("playerScoreHUDs");
                if (FadeInHUD > 0)
                    FadeIn(scoreHUDs[FadeInHUD - 1]);
                for (int i = 0; i < 10; i++)
                {
                    if (scoreList[i] == null)
                        scoreList[i] = Entity.GetEntity(i);
                    if (!scoreList[i].IsPlayer)
                    {
                        FadeOut(scoreHUDs[i]);
                        player.SetField("ScoreFadeout" + i, 1);
                        continue;
                    }
                    try
                    {
                        if (player.GetField<int>("ScoreFadeout" + i) == 0 && scoreList[i].GetField<string>("sessionteam") == "axis")
                        {
                            FadeOut(scoreHUDs[i]);
                            player.SetField("ScoreFadeout" + i, 1);
                        }
                        if (scoreList[i].GetField<string>("sessionteam") == "allies" && scoreHUDs[i].Alpha != 1)
                        {
                            scoreHUDs[i].Alpha = 1;
                        }
                        else if (scoreList[i].GetField<string>("sessionteam") == "allies")
                        {
                            scoreHUDs[i].Call("setvalue", scoreList[i].GetField<int>("cash"));
                        }
                        else
                        {
                            scoreHUDs[i].Alpha = 0;
                        }
                    }
                    catch
                    {
                        //scoreHUDs[i].Call("setvalue", 0);
                        scoreHUDs[i].Alpha = 0;
                    }
                }
            }
        }
        
        private void UpdateHUDAmmo(Entity player)
        {
            if (!player.HasField("bohud_created"))
            {
                return;
            }

            var ammoStock = player.GetField<HudElem>("bohud_ammoStock");
            var ammoClip = player.GetField<HudElem>("bohud_ammoClip");
            var ammoSlash = player.GetField<HudElem>("bohud_ammoSlash");
            var weaponName = player.GetField<HudElem>("bohud_weaponName");
            var weapon = player.CurrentWeapon;

            if (weapon == "riotshield_mp" || weapon == "scrambler_mp" || weapon == "iw5_riotshieldjugg_mp" || weapon == "claymore_mp")
            {
                ammoStock.Alpha = 0;
                ammoClip.Alpha = 0;
                ammoSlash.Alpha = 0;
            }
            else
            {
                ammoStock.Call("setvalue", player.GetWeaponAmmoStock(weapon));
                ammoClip.Call("setvalue", player.GetWeaponAmmoClip(weapon));
                ammoSlash.Alpha = 1;
                ammoStock.Alpha = 1;
                ammoClip.Alpha = 1;
            }

            switch (weapon)
            {
                case "iw5_usp45_mp":
                    weaponName.SetText("USP .45");
                    break;
                case "iw5_usp45_mp_akimbo":
                    weaponName.SetText("^3USP .45");
                    break;
                case "iw5_usp45_mp_akimbo_silencer02":
                    weaponName.SetText("^1Mustang & Sally");
                    break;
                case "iw5_p99_mp":
                    weaponName.SetText("P99");
                    break;
                case "iw5_p99_mp_xmags":
                    weaponName.SetText("^3P99");
                    break;
                case "iw5_p99_mp_tactical_xmags":
                    weaponName.SetText("^1Puncher99");
                    break;
                case "iw5_fnfiveseven_mp":
                    weaponName.SetText("Five Seven");
                    break;
                case "iw5_fnfiveseven_mp_xmags":
                    weaponName.SetText("^3Five Seven");
                    break;
                case "iw5_fnfiveseven_mp_akimbo_xmags":
                    weaponName.SetText("^1Fifty Seven");
                    break;
                case "iw5_deserteagle_mp":
                    weaponName.SetText("Desert Eagle");
                    break;
                case "iw5_deserteagle_mp_xmags":
                    weaponName.SetText("^3Desert Hawk");
                    break;
                case "iw5_deserteagle_mp_silencer02_xmags":
                    weaponName.SetText("^1Desert Snake");
                    break;
                case "iw5_mp412_mp":
                    weaponName.SetText("MP412");
                    break;
                case "iw5_mp412jugg_mp":
                    weaponName.SetText("^3Overlord #400");
                    break;
                case "iw5_mp412jugg_mp_xmags":
                    weaponName.SetText("^2Overlord #412");
                    break;
                case "iw5_44magnum_mp":
                    weaponName.SetText(".44 Magnum");
                    break;
                case "iw5_44magnum_mp_xmags":
                    weaponName.SetText("^3Anaconda");
                    break;
                case "iw5_44magnum_mp_akimbo_xmags":
                    weaponName.SetText("^1Anaconda X 2");
                    break;
                case "iw5_fmg9_mp":
                    weaponName.SetText("FMG9");
                    break;
                case "iw5_fmg9_mp_xmags":
                    weaponName.SetText("^3FMG9");
                    break;
                case "iw5_fmg9_mp_akimbo_xmags":
                    weaponName.SetText("^1Full Motion Glock 18");
                    break;
                case "iw5_g18_mp":
                    weaponName.SetText("G18");
                    break;
                case "iw5_g18_mp_xmags":
                    weaponName.SetText("^3G18");
                    break;
                case "iw5_g18_mp_silencer02_xmags":
                    weaponName.SetText("^1G19s");
                    break;
                case "iw5_skorpion_mp":
                    weaponName.SetText("Skorpion");
                    break;
                case "iw5_skorpion_mp_xmags":
                    weaponName.SetText("^3Tarantula");
                    break;
                case "iw5_skorpion_mp_akimbo_xmags":
                    weaponName.SetText("^1Tarantula & Cobra");
                    break;
                case "iw5_mp9_mp":
                    weaponName.SetText("MP9");
                    break;
                case "iw5_mp9_mp_xmags":
                    weaponName.SetText("^3MP9");
                    break;
                case "iw5_mp9_mp_reflexsmg_xmags":
                    weaponName.SetText("^1Meat Packer 9");
                    break;
                case "iw5_smaw_mp":
                    weaponName.SetText("SMAW");
                    break;
                case "rpg_mp":
                    weaponName.SetText("^1Role Playing Gun-7");
                    break;
                case "xm25_mp":
                    weaponName.SetText("XM25");
                    break;
                case "uav_strike_marker_mp":
                    weaponName.SetText("^1NZ25");
                    break;
                case "iw5_m4_mp":
                    weaponName.SetText("M4A1");
                    break;
                case "iw5_m4_mp_xmags_camo09":
                    weaponName.SetText("^3M4A1");
                    break;
                case "iw5_m4_mp_reflex_xmags_camo11":
                    weaponName.SetText("^1Mad4Assault");
                    break;
                case "iw5_m16_mp":
                    weaponName.SetText("M16");
                    break;
                case "iw5_m16_mp_xmags_camo09":
                    weaponName.SetText("^3M16");
                    break;
                case "iw5_m16_mp_rof_xmags_camo11":
                    weaponName.SetText("^1Skull Crusher 16");
                    break;
                case "iw5_cm901_mp":
                    weaponName.SetText("CM901");
                    break;
                case "iw5_cm901_mp_xmags_camo09":
                    weaponName.SetText("^3CM901");
                    break;
                case "iw5_cm901_mp_acog_xmags_camo11":
                    weaponName.SetText("^1Crush Manager 991");
                    break;
                case "iw5_type95_mp":
                    weaponName.SetText("Type 95");
                    break;
                case "iw5_type95_mp_xmags_camo09":
                    weaponName.SetText("^3Type 95");
                    break;
                case "iw5_type95_mp_reflex_xmags_camo11":
                    weaponName.SetText("^1Type 190");
                    break;
                case "iw5_acr_mp":
                    weaponName.SetText("ACR 6.8");
                    break;
                case "iw5_acr_mp_xmags_camo09":
                    weaponName.SetText("^3ACR 6.8");
                    break;
                case "iw5_acr_mp_eotech_xmags_camo11":
                    weaponName.SetText("^1Masada 1216");
                    break;
                case "iw5_mk14_mp":
                    weaponName.SetText("MK14");
                    break;
                case "iw5_mk14_mp_xmags_camo09":
                    weaponName.SetText("^3MK14");
                    break;
                case "iw5_mk14_mp_reflex_xmags_camo11":
                    weaponName.SetText("^1Massive Killer 28");
                    break;
                case "iw5_ak47_mp":
                    weaponName.SetText("AK-47");
                    break;
                case "iw5_ak47_mp_xmags_camo09":
                    weaponName.SetText("^3AK-47");
                    break;
                case "iw5_ak47_mp_gp25_xmags_camo11":
                    weaponName.SetText("^1AK74G");
                    break;
                case "iw5_g36c_mp":
                    weaponName.SetText("G36C");
                    break;
                case "iw5_g36c_mp_xmags_camo09":
                    weaponName.SetText("^3G36C");
                    break;
                case "iw5_g36c_mp_hybrid_xmags_camo11":
                    weaponName.SetText("^1G36 Capper");
                    break;
                case "iw5_scar_mp":
                    weaponName.SetText("SCAR-L");
                    break;
                case "iw5_scar_mp_xmags_camo09":
                    weaponName.SetText("^3SCAR-L");
                    break;
                case "iw5_scar_mp_eotech_xmags_camo11":
                    weaponName.SetText("^1Facial SCAR");
                    break;
                case "iw5_fad_mp":
                    weaponName.SetText("FAD");
                    break;
                case "iw5_fad_mp_xmags_camo09":
                    weaponName.SetText("^3FAD");
                    break;
                case "iw5_fad_mp_m320_xmags_camo11":
                    weaponName.SetText("^1Functional Annihilation Device");
                    break;
                case "iw5_mp5_mp":
                    weaponName.SetText("MP5");
                    break;
                case "iw5_mp5_mp_xmags_camo09":
                    weaponName.SetText("^3MP5");
                    break;
                case "iw5_mp5_mp_reflexsmg_xmags_camo11":
                    weaponName.SetText("^1craMP5");
                    break;
                case "iw5_ump45_mp":
                    weaponName.SetText("UMP45");
                    break;
                case "iw5_ump45_mp_xmags_camo09":
                    weaponName.SetText("^3UMP45");
                    break;
                case "iw5_ump45_mp_eotechsmg_xmags_camo11":
                    weaponName.SetText("^1U45 Hologram");
                    break;
                case "iw5_pp90m1_mp":
                    weaponName.SetText("PP90M1");
                    break;
                case "iw5_pp90m1_mp_xmags_camo09":
                    weaponName.SetText("^3PP90M1");
                    break;
                case "iw5_pp90m1_mp_silencer_xmags_camo11":
                    weaponName.SetText("^1PeePee90Mark1");
                    break;
                case "iw5_p90_mp":
                    weaponName.SetText("P90");
                    break;
                case "iw5_p90_mp_xmags_camo09":
                    weaponName.SetText("^3P90");
                    break;
                case "iw5_p90_mp_rof_xmags_camo11":
                    weaponName.SetText("^1Passive Aggressor");
                    break;
                case "iw5_m9_mp":
                    weaponName.SetText("PM-9");
                    break;
                case "iw5_m9_mp_xmags_camo09":
                    weaponName.SetText("^3PM-9");
                    break;
                case "iw5_m9_mp_thermalsmg_xmags_camo11":
                    weaponName.SetText("^1Suzi-Cue");
                    break;
                case "iw5_mp7_mp":
                    weaponName.SetText("MP7");
                    break;
                case "iw5_mp7_mp_xmags_camo09":
                    weaponName.SetText("^3MP7");
                    break;
                case "iw5_mp7_mp_silencer_xmags_camo11":
                    weaponName.SetText("^1Mortal Punisher 7");
                    break;
                case "iw5_dragunov_mp_dragunovscope":
                    weaponName.SetText("Dragunov");
                    break;
                case "iw5_dragunov_mp_dragunovscope_xmags_camo09":
                    weaponName.SetText("^3Dragunov");
                    break;
                case "iw5_dragunov_mp_acog_xmags_camo11":
                    weaponName.SetText("^1DragonBreath");
                    break;
                case "iw5_barrett_mp_barrettscope":
                    weaponName.SetText("Barrett .50 Cal");
                    break;
                case "iw5_barrett_mp_barrettscope_xmags_camo09":
                    weaponName.SetText("^3Barrett .50 Cal");
                    break;
                case "iw5_barrett_mp_acog_xmags_camo11":
                    weaponName.SetText("^1Barrett Roller .55 Cal");
                    break;
                case "iw5_l96a1_mp_l96a1scope":
                    weaponName.SetText("L118A");
                    break;
                case "iw5_l96a1_mp_l96a1scope_xmags_camo09":
                    weaponName.SetText("^3L118A");
                    break;
                case "iw5_l96a1_mp_l96a1scopevz_xmags_camo11":
                    weaponName.SetText("^1L911C");
                    break;
                case "iw5_as50_mp_as50scope":
                    weaponName.SetText("AS50");
                    break;
                case "iw5_as50_mp_as50scope_xmags_camo09":
                    weaponName.SetText("^3AS50");
                    break;
                case "iw5_as50_mp_as50scopevz_xmags_camo11":
                    weaponName.SetText("^1AW-50");
                    break;
                case "iw5_rsass_mp_rsassscope":
                    weaponName.SetText("RSASS");
                    break;
                case "iw5_rsass_mp_rsassscope_xmags_camo09":
                    weaponName.SetText("^3RSASS");
                    break;
                case "iw5_rsass_mp_thermal_xmags_camo11":
                    weaponName.SetText("^1R's Ass");
                    break;
                case "iw5_msr_mp_msrscope":
                    weaponName.SetText("MSR");
                    break;
                case "iw5_msr_mp_msrscope_xmags_camo09":
                    weaponName.SetText("^3MSR");
                    break;
                case "iw5_msr_mp_msrscopevz_xmags_camo11":
                    weaponName.SetText("^1Mark SetteR");
                    break;
                case "iw5_sa80_mp":
                    weaponName.SetText("L86 LSW");
                    break;
                case "iw5_sa80_mp_xmags_camo09":
                    weaponName.SetText("^3L86 LSW");
                    break;
                case "iw5_sa80_mp_reflexlmg_xmags_camo11":
                    weaponName.SetText("^1Lasserator86");
                    break;
                case "iw5_mg36_mp":
                    weaponName.SetText("MG36");
                    break;
                case "iw5_mg36_mp_xmags_camo09":
                    weaponName.SetText("^3MG36");
                    break;
                case "iw5_mg36_mp_grip_xmags_camo11":
                    weaponName.SetText("^1Masseration Gun 72");
                    break;
                case "iw5_pecheneg_mp":
                    weaponName.SetText("PKP Pecheneg");
                    break;
                case "iw5_pecheneg_mp_xmags_camo09":
                    weaponName.SetText("^3PKP Pecheneg");
                    break;
                case "iw5_pecheneg_mp_thermal_xmags_camo11":
                    weaponName.SetText("^1PKP Pet-ur-egg");
                    break;
                case "iw5_mk46_mp":
                    weaponName.SetText("MK46");
                    break;
                case "iw5_mk46_mp_xmags_camo09":
                    weaponName.SetText("^3MK46");
                    break;
                case "iw5_mk46_mp_silencer_xmags_camo11":
                    weaponName.SetText("^1MarKer902");
                    break;
                case "iw5_m60_mp":
                    weaponName.SetText("M60E4");
                    break;
                case "iw5_m60_mp_xmags_camo09":
                    weaponName.SetText("^3M60E4");
                    break;
                case "iw5_m60jugg_mp_reflexlmg_xmags":
                    weaponName.SetText("^2Manhandler120");
                    break;
                case "iw5_m60jugg_mp_eotechlmg_camo07":
                    weaponName.SetText("^2AUG HBAR");
                    break;
                case "iw5_m60jugg_mp_eotechlmg_silencer_camo06":
                    weaponName.SetText("^3AUG HBAR");
                    break;
                case "iw5_m60jugg_mp_silencer_thermal_camo08":
                    weaponName.SetText("^2AUX CrowBAR");
                    break;
                case "m320_mp":
                    weaponName.SetText("M320 GLM");
                    break;
                case "iw5_usas12_mp":
                    weaponName.SetText("USAS-12");
                    break;
                case "iw5_usas12_mp_xmags_camo09":
                    weaponName.SetText("^3USAS-12");
                    break;
                case "iw5_usas12_mp_reflex_xmags_camo11":
                    weaponName.SetText("^1USedASs-24");
                    break;
                case "iw5_ksg_mp":
                    weaponName.SetText("KSG");
                    break;
                case "iw5_ksg_mp_xmags_camo09":
                    weaponName.SetText("^3KSG");
                    break;
                case "iw5_ksg_mp_grip_xmags_camo11":
                    weaponName.SetText("^1Killing Spree Gun");
                    break;
                case "iw5_spas12_mp":
                    weaponName.SetText("SPAS-12");
                    break;
                case "iw5_spas12_mp_xmags_camo09":
                    weaponName.SetText("^3SPAS-12");
                    break;
                case "iw5_spas12_mp_grip_xmags_camo11":
                    weaponName.SetText("^1SPAZ-24");
                    break;
                case "iw5_striker_mp":
                    weaponName.SetText("Striker");
                    break;
                case "iw5_striker_mp_xmags_camo09":
                    weaponName.SetText("^3Striker");
                    break;
                case "iw5_striker_mp_grip_xmags_camo11":
                    weaponName.SetText("^1Strike-Out");
                    break;
                case "iw5_aa12_mp":
                    weaponName.SetText("AA12");
                    break;
                case "iw5_aa12_mp_xmags_camo09":
                    weaponName.SetText("^3AA12");
                    break;
                case "iw5_aa12_mp_grip_xmags_camo11":
                    weaponName.SetText("^1AutoAssassinator24");
                    break;
                case "iw5_1887_mp":
                    weaponName.SetText("Model 1887");
                    break;
                case "iw5_1887_mp_camo09":
                    weaponName.SetText("^3Model 1887");
                    break;
                case "iw5_1887_mp_camo11":
                    weaponName.SetText("^1Model 1337");
                    break;
                case "riotshield_mp":
                    weaponName.SetText("Riot Shield");
                    break;
                case "iw5_riotshieldjugg_mp":
                    weaponName.SetText("^1Reinforced Internal Optimal Titanium Shield");
                    break;
                case "gl_mp":
                    weaponName.SetText("^1M640");
                    break;
                case "scrambler_mp":
                    weaponName.SetText("^1Zombie Infection Device");
                    break;
                case "iw5_skorpion_mp_eotechsmg":
                    weaponName.SetText("^2Ray Gun");
                    break;
                case "iw5_skorpion_mp_eotechsmg_xmags":
                    weaponName.SetText("^1Porter's X2 Ray Gun");
                    break;
                case "defaultweapon_mp":
                    weaponName.SetText("^2Hand-gun");
                    break;
                case "claymore_mp":
                    weaponName.SetText("Claymore");
                    break;
            }
        }
        public void initRoundsLives(Entity player)
        {
            /*
            player.OnInterval(1000, (p) =>
                {
                    if (p.GetField<string>("sessionteam") != "allies")
                    {
                        
                        //PrisonBreak(p);//This was previously called every time player was in jail. Notifys stay forever, so only need this once. Fixes multi-text bug.
                        return false;
                    }
                    return true;
                });
             */
                player.SetField("Lives", 10);
            roundCounter(player);
        }
        public void roundCounter(Entity player)
        {
            HudElem round = HudElem.CreateFontString(player, "hudbig", 2);
            round.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 10, -45); //25 original
            round.HideWhenInMenu = true;
            //round.SetText("^1ERROR");
            round.GlowAlpha = 0.9f;
            round.GlowColor = new Vector3(0.5f, 0, 0);
            round.Color = new Vector3(.9f, 0, 0);
            //round.SetText("^1" + RoundNo.ToString());
            round.Call("setvalue", RoundNo);
            player.SetField("hud_roundCounter", new Parameter(round));
            //if (RoundNo > 19)
                //round.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 35, -5);
            /*
            OnInterval(1000, () =>
            {
                //round.Call("setvalue", RoundNo);
                //round.SetText("^1" + RoundNo.ToString());//Possibly fixes a bit of instability then using setvalue. Also adds red number.
                
                return true;
            });
             */
            
        }

        private void FadeRound(HudElem hud, Entity player)
        {
            hud.Call("fadeovertime", 1);
            hud.Alpha = 0;
            RoundNo = player.GetField<int>("RoundNo");
            AfterDelay(1000, () =>
                {
                    hud.Call("setvalue", RoundNo);
                    hud.Call("fadeovertime", 1);
                    hud.Alpha = 1;
                });
        }
        private void FadeOut(HudElem hud)
        {
            hud.Call("fadeovertime", 1);
            hud.Alpha = 0;
        }
        private void FadeIn(HudElem hud)
        {
            hud.Call("fadeovertime", 1);
            hud.Alpha = 1;
        }
        public void LifeHandler(Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis")
            {
                player.SetField("hasHealthHud", 1);
                HudElem livetext = HudElem.CreateFontString(player, "hudbig", 0.9f);
                livetext.SetPoint("TOP LEFT", "TOP LEFT", 5, 5);
                livetext.HideWhenInMenu = true;
                livetext.Archived = true;
                HudElem lives = HudElem.CreateFontString(player, "hudbig", 0.9f);
                lives.SetPoint("TOP LEFT", "TOP LEFT", 100, 5);
                lives.HideWhenInMenu = true;
                lives.Parent = livetext;

                HudElem healthtext = HudElem.CreateFontString(player, "hudbig", 0.9f);
                healthtext.SetPoint("TOP LEFT", "TOP LEFT", 5, 25);
                healthtext.HideWhenInMenu = true;
                healthtext.Archived = true;
                HudElem health = HudElem.CreateFontString(player, "hudbig", 0.9f);
                health.SetPoint("TOP LEFT", "TOP LEFT", 100, 25);
                health.HideWhenInMenu = true;
                health.Parent = healthtext;

                player.SetField("hud_health", new Parameter(healthtext));
                player.SetField("hud_lives", new Parameter(livetext));

                livetext.SetText("Lives:");
                healthtext.SetText("Health:");
                int Lives = player.GetField<int>("Lives");
                //int Health = player.Health;
                AfterDelay(500, () =>
                    {
                        lives.Call(32963, Lives);
                        health.Call(32963, ZombieHealth);
                    });
                /*
                OnInterval(1000, () =>
                    {
                        int Lives = player.GetField<int>("Lives");
                        int Health = player.Health;
                        //livetext.SetText("Lives: " + Lives);
                        //health.SetText("Health: " + Health);
                        lives.Call(32963, Lives);
                        health.Call(32963, Health);
                        return true;
                    });
                 */
            }
        }

        public void ZombieChecker(Entity player)
        {
            player.SetField("maxhealth", ZombieHealth);
            player.Health = ZombieHealth;
            player.SetPerk("specialty_gpsjammer", true, true);
            player.SetPerk("specialty_coldblooded", true, true);
            //player.SetPerk("specialty_", true, true);
            if (player.GetField<string>("sessionteam") == "axis" && !TotalZombies.Contains(player))
            {
                TotalZombies.Add(player);
            }
            if (player.GetField<int>("Lives") == 0)
            {
                ZombiesDead.Add(player);
                ZombWaitForNextRound(player);
            }
            int aliveZombies = Call<int>(318, "axis");
            if (ZombiesDead.Count == TotalZombies.Count && ZombiesDead.Count == aliveZombies)
                Notify("zombiesDied");
        }

        public void ZombWaitForNextRound(Entity player)
        {
            if (player.GetField<int>("Lives") != 0) return;
            player.OnInterval(1000, (p) =>
                {
                    if (p.IsAlive && p.GetField<int>("Lives") == 0)
                    {
                        SpawnInZombiePrison(p);
                        p.SetField("Lives", 10);
                        p.Call("iprintlnbold", "^1You have lost all your lives.");
                        p.AfterDelay(4000, (p2) =>
                            p2.Call("iprintlnbold", "^1Wait for the other zombies and for the next round."));
                        return false;
                    }
                    else return true;
                });
            player.OnInterval(500, (p) =>
            {
                int aliveZombies = Call<int>(318, "axis");
                if (ZombiesDead.Count == TotalZombies.Count && ZombiesDead.Count == aliveZombies)
                {
                    //PrisonBreak(player);
                    //Notify("zombiesDied");
                    ZombiesDead.Clear();
                    ZombieHealth = ZombieHealth + 50;
                    player.SetField("maxhealth", ZombieHealth);
                    player.Health = ZombieHealth;
                    return false;
                }
                else return true;
            });
        }
        public void initPrisonBreak()
        {
                OnNotify("zombiesDied", () =>
                    {
                        foreach (Entity player in Players) if (player.IsAlive && player.GetField<string>("sessionteam") == "axis") player.Call("iprintlnbold", "All zombies have died. Wait for the next round to start.");
                    });
                OnNotify("RoundStart", () =>
                    {
                        int totalZombies = Call<int>("getteamplayersalive", "axis");
                        if (totalZombies == 1)
                        {
                            foreach (Entity player in Players) if (player.IsAlive && player.GetField<string>("sessionteam") == "axis") player.SetField("Lives", 15);
                        }
                        foreach (Entity player in Players) if (player.IsAlive && player.GetField<string>("sessionteam") == "axis") GetRandomSpawnForMap(player);
                    });
        }

        private void updatePerkHUD(Entity player, int Gambler)
        {
            string Perk = player.GetField<string>("PerkBought");
            var jugg = player.GetField<HudElem>("juggHUD");
            var stamina = player.GetField<HudElem>("staminaHUD");
            var speed = player.GetField<HudElem>("speedHUD");
            var mulekick = player.GetField<HudElem>("mulekickHUD");
            var dtap = player.GetField<HudElem>("dtapHUD");
            var stalker = player.GetField<HudElem>("stalkerHUD");
            var perk7 = player.GetField<HudElem>("perk7HUD");
            var perk8 = player.GetField<HudElem>("perk8HUD");

            int hasJuggHUD = player.GetField<int>("juggHUDDone");
            int hasStaminaHUD = player.GetField<int>("staminaHUDDone");
            int hasSpeedHUD = player.GetField<int>("speedHUDDone");
            int hasMulekickHUD = player.GetField<int>("mulekickHUDDone");
            int hasDtapHUD = player.GetField<int>("dtapHUDDone");
            int hasStalkerHUD = player.GetField<int>("stalkerHUDDone");
            int hasPerk7HUD = player.GetField<int>("PERK7HUDDone");
            int hasPerk8HUD = player.GetField<int>("PERK8HUDDone");

            if (player.IsAlive)
            {
                if (Gambler == 1)
                {
                    jugg.Alpha = 0;
                    jugg.SetShader("", 30, 30);
                    stamina.Alpha = 0;
                    stamina.SetShader("", 30, 30);
                    speed.Alpha = 0;
                    speed.SetShader("", 30, 30);
                    mulekick.Alpha = 0;
                    mulekick.SetShader("", 30, 30);
                    dtap.Alpha = 0;
                    dtap.SetShader("", 30, 30);
                    stalker.Alpha = 0;
                    stalker.SetShader("", 30, 30);
                    perk7.Alpha = 0;
                    perk7.SetShader("", 30, 30);
                    perk8.Alpha = 0;
                    perk8.SetShader("", 30, 30);
                    player.SetField("juggHUDDone", 0);
                    player.SetField("staminaHUDDone", 0);
                    player.SetField("speedHUDDone", 0);
                    player.SetField("mulekickHUDDone", 0);
                    player.SetField("dtapHUDDone", 0);
                    player.SetField("stalkerHUDDone", 0);
                    player.SetField("PERK7HUDDone", 0);
                    player.SetField("PERK8HUDDone", 0);
                }
                else if (hasJuggHUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            jugg.SetShader(Perk, 30, 30);
                            stamina.Alpha = 0;
                            speed.Alpha = 0;
                            mulekick.Alpha = 0;
                            dtap.Alpha = 0;
                            stalker.Alpha = 0;
                            perk7.Alpha = 0;
                            perk8.Alpha = 0;
                            jugg.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("juggHUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 0 && hasSpeedHUD == 0 && hasMulekickHUD == 0 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            stamina.SetShader(Perk, 30, 30);
                            stamina.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("staminaHUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 0 && hasMulekickHUD == 0 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            speed.SetShader(Perk, 30, 30);
                            speed.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("speedHUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 0 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            mulekick.SetShader(Perk, 30, 30);
                            mulekick.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("mulekickHUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            dtap.SetShader(Perk, 30, 30);
                            dtap.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("dtapHUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 1 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            stalker.SetShader(Perk, 30, 30);
                            stalker.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("stalkerHUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 1 && hasStalkerHUD == 1 && hasPerk7HUD == 0 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            perk7.SetShader(Perk, 30, 30);
                            perk7.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("PERK7HUDDone", 1));
                }
                else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 1 && hasStalkerHUD == 1 && hasPerk7HUD == 1 && hasPerk8HUD == 0 && Gambler == 0)
                {
                    AfterDelay(9050, () =>
                        {
                            perk8.SetShader(Perk, 30, 30);
                            perk8.Alpha = 1;
                            
                        });
                    AfterDelay(3000, () =>
                        player.SetField("PERK8HUDDone", 1));
                }
            }
            else
            {
                jugg.Alpha = 0;
                jugg.SetShader("", 30, 30);
                stamina.Alpha = 0;
                stamina.SetShader("", 30, 30);
                speed.Alpha = 0;
                speed.SetShader("", 30, 30);
                mulekick.Alpha = 0;
                mulekick.SetShader("", 30, 30);
                dtap.Alpha = 0;
                dtap.SetShader("", 30, 30);
                stalker.Alpha = 0;
                stalker.SetShader("", 30, 30);
                perk7.Alpha = 0;
                perk7.SetShader("", 30, 30);
                perk8.Alpha = 0;
                perk8.SetShader("", 30, 30);
                player.SetField("juggHUDDone", 0);
                player.SetField("staminaHUDDone", 0);
                player.SetField("speedHUDDone", 0);
                player.SetField("mulekickHUDDone", 0);
                player.SetField("dtapHUDDone", 0);
                player.SetField("stalkerHUDDone", 0);
                player.SetField("PERK7HUDDone", 0);
                player.SetField("PERK8HUDDone", 0);
            }
        }

        public int GetPerkPath(Entity player)
        {
            int hasJuggHUD = player.GetField<int>("juggHUDDone");
            int hasStaminaHUD = player.GetField<int>("staminaHUDDone");
            int hasSpeedHUD = player.GetField<int>("speedHUDDone");
            int hasMulekickHUD = player.GetField<int>("mulekickHUDDone");
            int hasDtapHUD = player.GetField<int>("dtapHUDDone");
            int hasStalkerHUD = player.GetField<int>("stalkerHUDDone");
            int hasPerk7HUD = player.GetField<int>("PERK7HUDDone");
            int hasPerk8HUD = player.GetField<int>("PERK8HUDDone");
            if (hasJuggHUD == 1 && hasStaminaHUD == 0 && hasSpeedHUD == 0 && hasMulekickHUD == 0 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0)
                return -378;
            else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 0 && hasMulekickHUD == 0 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0)
                return -346;
            else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 0 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0)
                return -314;
            else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 0 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0)
                return -282;
            else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 1 && hasStalkerHUD == 0 && hasPerk7HUD == 0 && hasPerk8HUD == 0)
                return -250;
            else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 1 && hasStalkerHUD == 1 && hasPerk7HUD == 0 && hasPerk8HUD == 0)
                return -218;
            else if (hasJuggHUD == 1 && hasStaminaHUD == 1 && hasSpeedHUD == 1 && hasMulekickHUD == 1 && hasDtapHUD == 1 && hasStalkerHUD == 1 && hasPerk7HUD == 1 && hasPerk8HUD == 0)
                return -186;
            else return -410;
        }

        public void HandleUpgradeSpecialWeps(Entity player)
        {
            player.OnNotify("weapon_fired", (self, weapon) =>
            {
                if (weapon.As<string>() == "iw5_usp45_mp_akimbo_silencer02")
                {
                    Vector3 asd = Call<Vector3>("anglestoforward", player.Call<Vector3>("getplayerangles"));
                    Vector3 dsa = new Vector3(asd.X * 1000000, asd.Y * 1000000, asd.Z * 1000000);
                    Call("magicbullet", "gl_mp", player.Call<Vector3>("gettagorigin", "tag_weapon_left"), dsa, self);
                }
                if (weapon.As<string>() == "rpg_mp")
                {
                    Vector3 asd = Call<Vector3>("anglestoforward", player.Call<Vector3>("getplayerangles"));
                    Vector3 dsa = new Vector3(asd.X * 1000000, asd.Y * 1000000, asd.Z * 1000000);
                    Call("magicbullet", "rpg_mp", player.Call<Vector3>("gettagorigin", "tag_weapon_left"), dsa, self);
                }
                if (weapon.As<string>() == "uav_strike_marker_mp")
                {
                    Vector3 asd = Call<Vector3>("anglestoforward", player.Call<Vector3>("getplayerangles"));
                    Vector3 dsa = new Vector3(asd.X * 1000000, asd.Y * 1000000, asd.Z * 1000000);
                    Call("magicbullet", "xm25_mp", player.Call<Vector3>("gettagorigin", "tag_weapon_left"), dsa, self);
                    OnInterval(100, () =>
                        {
                            player.Call("setweaponammostock", "uav_strike_marker_mp", 2);
                            return true;
                        });
                }
                if (weapon.As<string>() == "iw5_type95_mp_reflex_xmags_camo11" || weapon.As<string>() == "iw5_m16_mp_rof_xmags_camo11")
                {
                    player.SetClientDvar("player_burstFireCooldown","0");
                }
                if (weapon.As<string>() != "iw5_type95_mp_reflex_xmags_camo11" || weapon.As<string>() != "iw5_m16_mp_rof_xmags_camo11")
                {
                    player.SetClientDvar("player_burstFireCooldown", "0.2");
                }
                /*
                if (weapon.As<string>() == "iw5_skorpion_mp_eotechsmg")
                {
                    Vector3 playerForward = player.Call<Vector3>("gettagorigin", "tag_weapon") + Call<Vector3>("AnglesToForward", player.Call<Vector3>("getplayerangles")) * 100000;
                    Entity refobject = Call<Entity>("spawn", "script_model", player.Call<Vector3>("gettagorigin", "tag_weapon_left"));
                    refobject.Call("setmodel", "tag_origin");
                    refobject.SetField("angles", player.Call<Vector3>("getplayerangles"));
                    Call("playfxontag", RaygunShot, refobject, "tag_origin");
                    refobject.Call("moveto", playerForward, 70);
                    refobject.OnInterval(10, (refent) =>
                    {
                        if (CollidingSoon(refent, player))
                        {
                            Entity redfx = Call<Entity>("spawnfx", RaygunImpactFX, refent.Origin);
                            Call("triggerfx", redfx);
                            Call("radiusdamage", redfx.Origin, 10, 60, 60, player);
                            refobject.Call("delete");
                            AfterDelay(500, () => { redfx.Call("delete"); });
                            return false;
                        }

                        return true;
                    });
                }
                if (weapon.As<string>() == "iw5_skorpion_mp_eotechsmg_xmags")
                {
                    Vector3 playerForward = player.Call<Vector3>("gettagorigin", "tag_weapon") + Call<Vector3>("AnglesToForward", player.Call<Vector3>("getplayerangles")) * 100000;
                    Entity refobject = Call<Entity>("spawn", "script_model", player.Call<Vector3>("gettagorigin", "tag_weapon_left"));
                    refobject.Call("setmodel", "tag_origin");
                    refobject.SetField("angles", player.Call<Vector3>("getplayerangles"));
                    Call("playfxontag", RaygunShotU, refobject, "tag_origin");
                    refobject.Call("moveto", playerForward, 100);
                    refobject.OnInterval(10, (refent) =>
                    {
                        if (CollidingSoon(refent, player))
                        {
                            Entity redfx = Call<Entity>("spawnfx", RaygunImpactFXU, refent.Origin);
                            Call("triggerfx", redfx);
                            Call("radiusdamage", redfx.Origin, 10, 60, 60, player);
                            refobject.Call("delete");
                            AfterDelay(500, () => { redfx.Call("delete"); });
                            return false;
                        }

                        return true;
                    });
                }
                 */
            });
        }

        public Entity randomWeaponCrate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active", new Parameter(crate.Origin), "cardicon_league_1911"); // objective_add
            //Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            //Call(434, curObjID, "cardicon_league_1911"); // objective_icon
            Call(358, curObjID, "allies");
            HudElem HeadIcon = HudElem.NewHudElem();
            HeadIcon.X = origin.X;
            HeadIcon.Y = origin.Y;
            HeadIcon.Z = origin.Z + 40;
            HeadIcon.Alpha = 0.85f;
            HeadIcon.SetShader("weapon_colt_45", 10, 10);
            HeadIcon.Call("setwaypoint", true, true, false);
            crate.SetField("state", "idle");
            crate.SetField("giveweapon", "");
            crate.SetField("player", "");
            MakeUsable(crate, "randombox", 75);
            return crate;
        }

        public Entity papCrate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_enemy");
            crate.Call("clonebrushmodeltoscriptmodel", _airdropCollision);
            crate.SetField("angles", new Parameter(angles));
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "cardicon_brassknuckles"); // objective_icon
            Call(358, curObjID, "allies");
            HudElem HeadIcon = HudElem.NewHudElem();
            HeadIcon.X = origin.X;
            HeadIcon.Y = origin.Y;
            HeadIcon.Z = origin.Z + 40;
            HeadIcon.Alpha = 0.85f;
            HeadIcon.SetShader("cardicon_brassknuckles", 10, 10);
            HeadIcon.Call("setwaypoint", true, true, false);
            MakeUsable(crate, "pap", 75);
            return crate;
        }

        public Entity upgradeCrate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_enemy");
            crate.Call("clonebrushmodeltoscriptmodel", _airdropCollision);
            crate.SetField("angles", new Parameter(angles));
            MakeUsable(crate, "upgradebox", 75);
            return crate;
        }

        public Entity gamblerCrate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.Call("clonebrushmodeltoscriptmodel", _airdropCollision);
            crate.SetField("angles", new Parameter(angles));
            Entity laptop = Call<Entity>("spawn", "script_model", new Vector3(origin.X, origin.Y, origin.Z + 22));
            laptop.SetField("angles", new Parameter(angles - new Vector3(0, 90, 0)));
            laptop.Call("setmodel", "com_laptop_2_open");
            OnInterval(1000, () =>
                {
                    laptop.Call(33408, 360, 4);
                    return true;
                });
            OnNotify("GamblerUse", () =>
                {
                    laptop.Call(33399, new Vector3(origin.X, origin.Y, origin.Z + 38), 4);
                });
            OnNotify("GamblerDone", () =>
                {
                    laptop.Call(33399, new Vector3(origin.X, origin.Y, origin.Z + 22), 4);
                });
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "cardicon_8ball"); // objective_icon
            Call(358, curObjID, "allies");
            HudElem HeadIcon = HudElem.NewHudElem();
            HeadIcon.X = origin.X;
            HeadIcon.Y = origin.Y;
            HeadIcon.Z = origin.Z + 40;
            HeadIcon.Alpha = 0.85f;
            HeadIcon.SetShader("cardicon_8ball", 10, 10);
            HeadIcon.Call("setwaypoint", true, true, false);
            MakeUsable(crate, "gambler", 75);
            return crate;
        }

        public Entity Perk1Crate(Vector3 origin, Vector3 angles, bool Interchange)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_enemy");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "cardicon_juggernaut_1"); // objective_icon
            Call(358, curObjID, "allies");
            if (!Interchange)
                MakeUsable(crate, "perk1", 75);
            else
            {
                //InitJuggAnim(crate);//currently passed through RebarSpawn
                SpawnRebar(origin, crate);
            }
            return crate;
        }
        public void SpawnRebar(Vector3 origin, Entity crate)
        {
            Entity rebar = Call<Entity>("spawn", "script_model", new Parameter(origin - new Vector3(0, 100, 45)));
            rebar.Call("setmodel", "concrete_slabs_lrg1");
            AfterDelay(120000, () =>
            InitJuggAnim(crate, rebar));
        }
        public Entity Perk2Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_enemy");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "specialty_longersprint"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk2", 75);
            return crate;
        }

        public Entity Perk3Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "specialty_fastreload"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk3", 75);
            return crate;
        }

        public Entity Perk4Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "specialty_twoprimaries"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk4", 75);
            return crate;
        }

        public Entity Perk5Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_enemy");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "cardicon_gears"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk5", 75);
            return crate;
        }

        public Entity Perk6Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "specialty_stalker"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk6", 75);
            return crate;
        }

        public Entity Perk7Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "specialty_scavenger"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk7", 75);
            return crate;
        }

        public Entity Perk8Crate(Vector3 origin, Vector3 angles)
        {
            Entity crate = Call<Entity>("spawn", "script_model", new Parameter(origin));
            crate.Call("setmodel", "com_plasticcase_friendly");
            crate.SetField("angles", new Parameter(angles));
            crate.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(crate.Origin)); // objective_position
            Call(434, curObjID, "cardicon_dive"); // objective_icon
            Call(358, curObjID, "allies");
            MakeUsable(crate, "perk8", 75);
            return crate;
        }

        public Entity Claymore(Vector3 origin, Vector3 angles)
        {
            Entity claymore = Call<Entity>("spawn", "script_model", new Parameter(origin));
            claymore.Call("setmodel", "weapon_claymore");
            claymore.SetField("angles", new Parameter(angles));
            int curObjID = 31 - _flagCount++;
            Call(431, curObjID, "active"); // objective_add
            Call(435, curObjID, new Parameter(claymore.Origin)); // objective_position
            Call(434, curObjID, "weapon_claymore"); // objective_icon
            Call(358, curObjID, "allies");
            HudElem HeadIcon = HudElem.NewHudElem();
            HeadIcon.X = origin.X;
            HeadIcon.Y = origin.Y;
            HeadIcon.Z = origin.Z + 15;
            HeadIcon.Alpha = 0.85f;
            HeadIcon.SetShader("weapon_claymore", 10, 10);
            HeadIcon.Call("setwaypoint", true, true, false);
            MakeUsable(claymore, "claymore", 70);
            return claymore;
        }

        public void WallWeapon(Vector3 origin, Vector3 angles, string weapon, int price)
        {
            Entity wep = Call<Entity>("spawn", "script_model", new Parameter(origin));
            string model = Call<string>(53, weapon);
            wep.Call("setmodel", model);
            wep.SetField("angles", new Parameter(angles));
            wep.SetField("price", price);
            wep.SetField("wep", weapon);
            MakeUsable(wep, "wallweapon", 70);
            //return wep;
        }

        public void ScorePopup(Entity player, int amount, string color, int client)
        {
            if (!player.IsPlayer || !player.IsAlive || player.GetField<string>("sessionteam") == "axis" || player.HasField("PointHUDDestroyed") || player.EntRef > 9) return;
                        updatePlayerScores(0);
                        HudElem score = HudElem.CreateFontString(player, "hudbig", 0.7f);

                        int[] PopupY = new int[10] { 150, 135, 120, 105, 90, 75, 60, 45, 30, 15 };
                        score.SetPoint("LEFTCENTER", "RIGHTCENTER", 285, PopupY[client]);
                        if (color == "G") score.Color = new Vector3(0.2f, 1, 0.2f);
                        else if (color == "R") score.Color = new Vector3(1, 0.2f, 0.2f);
                        else Log.Write(LogLevel.All, "Error in creating score popup color, wrong color value {0}", color);
                        int RandomX = Call<int>("randomintrange", -700, -300);
                        int RandomY = Call<int>("randomintrange", -500, 1000);

                        score.Call("setvalue", amount);
                        score.Alpha = 0.9f;
                        score.Call("setpulsefx", 80, 1500, 600);
                        score.Call("moveovertime", 90);
                        score.X = RandomX;
                        score.Y = RandomY;
                        AfterDelay(1600, () =>
                        {
                            AfterDelay(750, () =>
                            {
                                score.Call("destroy");
                            });
                        });
        }

        public void UsablesHud(Entity player)
        {
            HudElem message = HudElem.CreateFontString(player, "hudbig", 0.6f);
            message.SetPoint("CENTER", "CENTER", 0, 150);
            OnInterval(100, () =>
            {
                bool _changed = false;
                foreach (Entity ent in usables)
                {
                    if (player.GetField<string>("sessionteam") != "axis")
                    {
                        if (player.Origin.DistanceTo(ent.Origin) < ent.GetField<int>("range"))
                        {
                            string UseType = ent.GetField<string>("usabletype");
                            switch (UseType)
                            {
                                case "door":
                                    message.SetText(getDoorText(ent, player));
                                    break;
                                case "randombox":
                                    message.SetText(getBoxText(ent, player));
                                    break;
                                case "pap":
                                    message.SetText(getPapText(ent, player));
                                    break;
                                case "upgradebox":
                                    message.SetText(getUpgradeText(ent, player));
                                    break;
                                case "gambler":
                                    message.SetText(getGamblerText(ent, player));
                                    break;
                                case "perk1":
                                    message.SetText(getPerk1Text(ent, player));
                                    break;
                                case "perk2":
                                    message.SetText(getPerk2Text(ent, player));
                                    break;
                                case "perk3":
                                    message.SetText(getPerk3Text(ent, player));
                                    break;
                                case "perk4":
                                    message.SetText(getPerk4Text(ent, player));
                                    break;
                                case "perk5":
                                    message.SetText(getPerk5Text(ent, player));
                                    break;
                                case "perk6":
                                    message.SetText(getPerk6Text(ent, player));
                                    break;
                                case "perk7":
                                    message.SetText(getPerk7Text(ent, player));
                                    break;
                                case "perk8":
                                    message.SetText(getPerk8Text(ent, player));
                                    break;
                                case "claymore":
                                    message.SetText("Press ^3[{+activate}] ^7to buy Claymore [Cost: 500]");
                                    break;
                                case "wallweapon":
                                    message.SetText(getWallWeaponText(ent));
                                    break;
                                default:
                                    message.SetText("");
                                    break;
                            }
                            _changed = true;
                        }
                    }
                }
                if (!_changed)
                {
                    message.SetText("");
                }
                return true;
            });
        }

        public string getBoxText(Entity box, Entity player)
        {
            if (!player.GetField<string>("sessionteam").Equals("axis"))
            {
                if (box.GetField<string>("state").Equals("inuse")) return "";
                if (box.GetField<string>("state").Equals("waiting"))
                {
                    if (box.GetField<Entity>("player").Equals(player))
                        return "Press ^3[{+activate}] ^7to trade Weapons: " + localizedNames[box.GetField<int>("giveweapon")];
                    return "";
                }
                return "Press ^3[{+activate}] ^7for a Random Weapon [Cost: 950]";
            }
            return "";
        }

        public string getPapText(Entity box, Entity player)
        {
            if (!player.GetField<string>("sessionteam").Equals("axis")) return "Press ^3[{+activate}] ^7to buy Pack-A-Punch [Cost: 5000]";
            else return "";
        }
        public string getUpgradeText(Entity box, Entity player)
        {
            if (!player.GetField<string>("sessionteam").Equals("axis")) return "Press ^3[{+activate}] ^7to Half-Upgrade your Current Weapon [Cost: 2500]";
            else return "";
        }
        public string getGamblerText(Entity box, Entity player)
        {
            if (!player.GetField<string>("sessionteam").Equals("axis")) return "Press ^3[{+activate}] ^7to use the Gambler [Cost: 1000]";
            else return "";
        }
        public string getPerk1Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk1bought").Equals(1)) return "You already have Juggernog!";
            else return "Hold ^3[{+activate}] ^7to buy Juggernog [Cost: 2500]";
        }
        public string getPerk2Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk2bought").Equals(1)) return "You already have Stamin-Up!";
            else return "Hold ^3[{+activate}] ^7to buy Stamin-Up [Cost: 2000]";
        }
        public string getPerk3Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk3bought").Equals(1)) return "You already have Speed Cola!";
            else return "Hold ^3[{+activate}] ^7to buy Speed Cola [Cost: 3000]";
        }
        public string getPerk4Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk4bought").Equals(1)) return "You already have Mule Kick!";
            else return "Hold ^3[{+activate}] ^7to buy Mule Kick [Cost: 4000]";
        }
        public string getPerk5Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk5bought").Equals(1)) return "You already have Double Tap!";
            else return "Hold ^3[{+activate}] ^7to buy Double Tap [Cost: 2000]";
        }
        public string getPerk6Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk6bought").Equals(1)) return "You already have Stalker Soda!";
            else return "Hold ^3[{+activate}] ^7to buy Stalker Soda [Cost: 1500]";
        }
        public string getPerk7Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk7bought").Equals(1)) return "You already have Vulture Aid!";
            else return "Hold ^3[{+activate}] ^7to buy Vulture Aid [Cost: 3000]";
        }
        public string getPerk8Text(Entity box, Entity player)
        {
            if (player.GetField<int>("perk8bought").Equals(1)) return "You already have Diver's Splash!";
            else return "Hold ^3[{+activate}] ^7to buy Diver's Splash [Cost: 1000]";
        }
        public string getWallWeaponText(Entity box)
        {
            string weapon = box.GetField<string>("wep");
            string name = null;
            for (int i = 0; i < weaponNames.Length; i++)
            {
                if (weaponNames[i]+"_mp" == weapon)
                {
                    name = localizedNames[i];
                    break;
                }
            }
            if (name == null) name = weapon;
            return "Hold ^3[{+activate}] ^7to buy " + name + " [Cost: " + box.GetField<int>("price") + "]";
        }
        private string[] weaponModels = { "weapon_steyr_blue_tiger" , "weapon_usp45_iw5", "weapon_mp412", "weapon_desert_eagle_iw5",
            "weapon_ak47_iw5", "weapon_scar_iw5", "weapon_mp5_iw5", "weapon_p90_iw5",  "weapon_m60_iw5", "weapon_as50_iw5",
            "weapon_remington_msr_iw5",  "weapon_aa12_iw5", "weapon_model1887", "weapon_skorpion_iw5", "weapon_mp9_iw5", "weapon_walther_p99_iw5", "weapon_fn_fiveseven_iw5", "weapon_44_magnum_iw5", "weapon_fmg_iw5", "weapon_g18_iw5", "weapon_smaw",
            "weapon_xm25", "weapon_m320_gl", "weapon_m4_iw5", "weapon_m16_iw5", "weapon_cm901", "weapon_type95_iw5", "weapon_remington_acr_iw5", "weapon_m14_iw5", "weapon_g36_iw5", "weapon_fad_iw5", "weapon_ump45_iw5", "weapon_pp90m1_iw5", "weapon_uzi_m9_iw5", "weapon_mp7_iw5",
            "weapon_dragunov_iw5", "weapon_m82_iw5", "weapon_l96a1_iw5", "weapon_rsass_iw5", "weapon_sa80_iw5", "weapon_mg36", "weapon_pecheneg_iw5", "weapon_mk46_iw5", "weapon_usas12_iw5", "weapon_ksg_iw5", "weapon_spas12_iw5", "weapon_striker_iw5",
            "weapon_riot_shield_mp"
        };
        private string[] weaponNames = { "iw5_m60jugg_mp_eotechlmg_camo07", "iw5_usp45", "iw5_mp412", "iw5_deserteagle",
            "iw5_ak47", "iw5_scar", "iw5_mp5", "iw5_p90", "iw5_m60", "iw5_as50",
            "iw5_msr", "iw5_aa12", "iw5_1887", "iw5_skorpion", "iw5_mp9", "iw5_p99", "iw5_fnfiveseven", "iw5_44magnum", "iw5_fmg9", "iw5_g18", "iw5_smaw",
            "xm25", "m320", "iw5_m4", "iw5_m16", "iw5_cm901", "iw5_type95", "iw5_acr", "iw5_mk14", "iw5_g36c", "iw5_fad", "iw5_ump45", "iw5_pp90m1", "iw5_m9", "iw5_mp7",
            "iw5_dragunov", "iw5_barrett", "iw5_l96a1", "iw5_rsass", "iw5_sa80", "iw5_mg36", "iw5_pecheneg", "iw5_mk46", "iw5_usas12", "iw5_ksg", "iw5_spas12", "iw5_striker",
            "riotshield"
        };
        private string[] localizedNames = { "AUG HBAR", "USP .45", "MP412", "Desert Eagle",
            "AK-47", "SCAR-L", "MP5", "P90", "M60", "AS50",
            "MSR", "AA-12", "Model 1887", "Skorpion", "MP9", "P99", "Five Seven", "44. Magnum", "FMG9", "G18", "SMAW",
            "XM25", "M320 GLM", "M4A1", "M16", "CM901", "Type 95", "ACR 6.8", "MK14", "G36C", "FAD", "UMP45", "PP90M1", "PM-9", "MP7", 
            "Dragunov", "Barrett .50 Cal", "L118A", "RSASS", "L86 LSW", "MG36", "PKP Pecheneg", "MK46", "USAS-12", "KSG", "SPAS-12", "Striker", 
            "Riot Shield"
        };
        private bool _destroyed = false;
        
        public void usedBox(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.CurrentWeapon == "claymore_mp" || player.CurrentWeapon.Contains("killstreak")) return;
            if (box.GetField<string>("state").Equals("waiting") && box.GetField<Entity>("player").Equals(player) && player.GetField<int>("NewGunReady") == 1)
            {
                string name = Utilities.BuildWeaponName(weaponNames[box.GetField<int>("giveweapon")], "", "", 0, 0);
                if (box.GetField<int>("giveweapon") == 1 && player.GetField<int>("perk4bought") == 0)
                {
                    player.Call("givemaxammo", name);
                    player.Call("playlocalsound", "ammo_crate_use");
                    box.GetField<Entity>("weaponent").Call("delete");
                    _destroyed = true;
                    box.SetField("state", "idle");
                    return;
                }
                else
                {
                    player.GiveWeapon(name);
                    player.Call("givemaxammo", name);
                    player.SwitchToWeaponImmediate(name);
                    player.Call("playlocalsound", "ammo_crate_use");
                    box.GetField<Entity>("weaponent").Call("delete");
                    _destroyed = true;
                    player.SetField("NewGunReady", 0);
                    box.SetField("state", "idle");
                }
                return;
            }
            else if (box.GetField<string>("state").Equals("waiting") && box.GetField<Entity>("player").Equals(player) && player.GetField<int>("NewGunReady") == 0)
            {
                player.TakeWeapon(player.CurrentWeapon);
                string name = Utilities.BuildWeaponName(weaponNames[box.GetField<int>("giveweapon")], "", "", 0, 0);
                    player.GiveWeapon(name);
                    player.Call("givemaxammo", name);
                    player.SwitchToWeaponImmediate(name);
                    player.Call("playlocalsound", "ammo_crate_use");
                    box.GetField<Entity>("weaponent").Call("delete");
                    _destroyed = true;
                    box.SetField("state", "idle");
                return;
            }
            if (!box.GetField<string>("state").Equals("idle")) return;
            if (player.GetField<int>("cash") < 950) return;
            if (box.GetField<string>("state") == "idle")
            {
                player.SetField("cash", player.GetField<int>("cash") - 950);
                foreach (Entity players in Players)
                ScorePopup(players, 950, "R", player.EntRef);
            }
            box.SetField("state", "inuse");
            player.Call("playlocalsound", "achieve_bomb");
            Entity weapon = Call<Entity>("spawn", "script_model", new Parameter(new Vector3(box.Origin.X, box.Origin.Y, box.Origin.Z + 10)));
            box.SetField("weaponent", new Parameter(weapon));
            weapon.Call("setmodel", weaponModels[0]);
            int timecount = 0;
            int weapnum = 0;
            _destroyed = false;
            OnInterval(50, () =>
            {
                weapnum = _rng.Next(weaponModels.Length);
                weapon.Call("setmodel", weaponModels[weapnum]);
                Vector3 origin = weapon.Origin;
                weapon.Call(33399, new Vector3(origin.X, origin.Y, origin.Z + 0.37f), .05f); // moveto
                timecount++;
                if (timecount == 60) return false;
                return true;
            });
            AfterDelay(3000, () =>
            {
                box.SetField("state", "waiting");
                box.SetField("giveweapon", weapnum);
                weapon.Call(33399, new Vector3(box.Origin.X, box.Origin.Y, box.Origin.Z + 10), 10); // moveto
                box.SetField("player", player);
            });
            AfterDelay(13500, () =>
            {
                if (box.GetField<string>("state") != "idle" && weapon.Origin.Equals(new Vector3(box.Origin.X, box.Origin.Y, box.Origin.Z + 10)))
                {
                    if (!_destroyed)
                    {
                        weapon.Call("delete");
                        box.SetField("state", "idle");
                        _destroyed = true;
                    }
                }
            });
        }

        public void usedPapBox(Entity boxdude, Entity player)
        {
            if (player.GetField<int>("cash") == 5000 || (player.GetField<int>("cash") > 5000))
            {
                    string gun = (PapWeapon(player));
                    if (gun == null || gun == "")
                    {
                        player.Call("iPrintLnBold", string.Format("^1Invalid Weapon"));
                        player.AfterDelay(1500, (p) =>
                            p.Call("iPrintLnBold", string.Format("^1Either go to the Upgrade Box to upgrade again or replace weapon.")));
                        return;
                    }
                    player.TakeWeapon(player.CurrentWeapon);
                    player.GiveWeapon(gun);
                    player.SwitchToWeaponImmediate(gun);
                    player.SetField("cash", player.GetField<int>("cash") - 5000);
                foreach (Entity players in Players)
                    ScorePopup(players, 5000, "R", player.EntRef);
                    player.Call("givemaxammo", gun);
                    player.Call("playlocalsound", "ui_mp_nukebomb_timer");
            }
        }

        public void usedUpgradeBox(Entity boxdude, Entity player)
        {
            if (player.GetField<int>("cash") == 2500 || (player.GetField<int>("cash") > 2500))
            {
                string gun = (UpgradeWeapon(player));
                string pap = (PapWeapon(player));
                if (gun == null || gun == "")
                {
                    if (pap == null || pap == "")
                    {
                        player.Call("iPrintLnBold", string.Format("^1Invalid Weapon"));
                        return;
                    }
                    player.TakeWeapon(player.CurrentWeapon);
                    player.GiveWeapon(pap);
                    player.SwitchToWeaponImmediate(pap);
                    player.Call("givemaxammo", gun);
                }
                else
                {
                    player.TakeWeapon(player.CurrentWeapon);
                    player.GiveWeapon(gun);
                    player.SwitchToWeaponImmediate(gun);
                    player.Call("givemaxammo", gun);
                }
                player.SetField("cash", player.GetField<int>("cash") - 2500);
                foreach (Entity players in Players)
                ScorePopup(players, 2500, "R", player.EntRef);
                player.Call("playlocalsound", "ui_mp_nukebomb_timer");
            }
        }

        public void usedGambler(Entity boxdude, Entity player)
        {
            int? desiredNumber = null;
            if (player.GetField<string>("sessionteam") != "allies") return;
            if (player.GetField<int>("GamblerInUse") == 1 && player.GetField<int>("GamblerReady") == 0)
            {
                player.Call("iPrintLnBold", string.Format("Gambler is already in use!"));
                return;
            }
            if (player.GetField<int>("GamblerReady") == 0 && player.GetField<int>("GamblerInUse") == 0)
            {
                player.Call("iPrintLnBold", string.Format("You must wait 2 minutes in-between uses before using the Gambler!"));
                return;
            }
            if (player.GetField<int>("cash") == 1000 || (player.GetField<int>("cash") > 1000))
            {
                player.SetField("cash", player.GetField<int>("cash") - 1000);
                foreach (Entity players in Players)
                ScorePopup(players, 1000, "R", player.EntRef);
                player.SetField("GamblerInUse", 1);
                player.SetField("GamblerReady", 0);
                Notify("GamblerUse");
                player.Call("iPrintLnBold", string.Format("^2Your results will display in 10 seconds."));
                AfterDelay(1500, () =>
                {
                     player.Call("iPrintLnBold", string.Format("^210"));
                });
                AfterDelay(2500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^29"));
                });
                AfterDelay(3500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^28"));
                });
                AfterDelay(4500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^27"));
                });
                AfterDelay(5500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^26"));
                });
                AfterDelay(6500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^25"));
                });
                AfterDelay(7500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^24"));
                });
                AfterDelay(8500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^23"));
                });
                AfterDelay(9500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^22"));
                    Notify("GamblerDone");
                });
                AfterDelay(10500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^21"));
                });
                AfterDelay(11500, () =>
                    {
                        int? roll = new Random().Next(18);
                        if (desiredNumber != null)
                            roll = desiredNumber;
                        switch (roll)
                        {
                            case 0:
                                //Extra weapon
                                player.GiveWeapon("defaultweapon_mp");
                                AfterDelay(100, () =>
                                    player.SwitchToWeaponImmediate("defaultweapon_mp"));
                                player.Call("iPrintLnBold", string.Format("^2You've won an extra weapon slot."));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 1:
                                //500 points
                                player.SetField("cash", player.GetField<int>("cash") + 500);
                                foreach (Entity players in Players)
                                ScorePopup(players, 500, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 500 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 2:
                                //1000 points
                                player.SetField("cash", player.GetField<int>("cash") + 1000);
                                foreach (Entity players in Players)
                                ScorePopup(players, 1000, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 1000 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 3:
                                //1500 points
                                player.SetField("cash", player.GetField<int>("cash") + 1500);
                                foreach (Entity players in Players)
                                ScorePopup(players, 1500, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 1500 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 4:
                                //2000 points
                                player.SetField("cash", player.GetField<int>("cash") + 2000);
                                foreach (Entity players in Players)
                                ScorePopup(players, 2000, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 2000 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 5:
                                //5000 points
                                player.SetField("cash", player.GetField<int>("cash") + 5000);
                                foreach (Entity players in Players)
                                ScorePopup(players, 5000, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 5000 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 6:
                                //7500 points
                                player.SetField("cash", player.GetField<int>("cash") + 7500);
                                foreach (Entity players in Players)
                                ScorePopup(players, 7500, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 7500 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 7:
                                //10000 points
                                player.SetField("cash", player.GetField<int>("cash") + 10000);
                                foreach (Entity players in Players)
                                ScorePopup(players, 10000, "G", player.EntRef);
                                player.Call("iPrintLnBold", string.Format("^2You've won 10000 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                Call("playfx", MoneyFX, player.Origin);
                                break;
                            case 8:
                                //lose 500
                                player.SetField("cash", player.GetField<int>("cash") - 500);
                                foreach (Entity players in Players)
                                ScorePopup(players, 500, "R", player.EntRef);
                                if (player.GetField<int>("cash") < 0)
                                    player.SetField("cash", 0);
                                player.Call("iPrintLnBold", string.Format("^1You've lost 500 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 9:
                                //lose all perks
                                if (player.GetField<int>("perk1bought") == 1)
                                {
                                    player.SetField("maxhealth", 200);
                                    player.Health = 200;
                                    player.SetField("perk1bought", 0);
                                }
                                if (player.GetField<int>("perk2bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_lightweight", true);
                                    player.Call("unsetperk", "specialty_marathon", true);
                                    player.SetField("perk2bought", 0);
                                }
                                if (player.GetField<int>("perk3bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_fastreload", true);
                                    player.SetField("perk3bought", 0);
                                }
                                if (player.GetField<int>("perk4bought") == 1)
                                {
                                    player.TakeWeapon(player.CurrentWeapon);
                                    player.SetField("perk4bought", 0);
                                }
                                if (player.GetField<int>("perk5bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_rof", true);
                                    player.SetField("perk5bought", 0);
                                }
                                if (player.GetField<int>("perk6bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_stalker", true);
                                    player.SetField("perk6bought", 0);
                                }
                                if (player.GetField<int>("perk7bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_scavenger", true);
                                    player.SetField("perk7bought", 0);
                                }
                                if (player.GetField<int>("perk8bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_jumpdive", true);
                                    player.SetField("perk8bought", 0);
                                }
                                updatePerkHUD(player, 1);
                                player.Call("iPrintLnBold", string.Format("^1You've lost all of your perks!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 10:
                                //lose all perks and 200 points
                                player.SetField("cash", player.GetField<int>("cash") - 200);
                                foreach (Entity players in Players)
                                ScorePopup(players, 200, "R", player.EntRef);
                                if (player.GetField<int>("cash") < 0)
                                    player.SetField("cash", 0);
                                if (player.GetField<int>("perk1bought") == 1)
                                {
                                    player.SetField("maxhealth", 200);
                                    player.Health = 200;
                                    player.SetField("perk1bought", 0);
                                }
                                if (player.GetField<int>("perk2bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_lightweight", true);
                                    player.Call("unsetperk", "specialty_marathon", true);
                                    player.SetField("perk2bought", 0);
                                }
                                if (player.GetField<int>("perk3bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_fastreload", true);
                                    player.SetField("perk3bought", 0);
                                }
                                if (player.GetField<int>("perk4bought") == 1)
                                {
                                    player.TakeWeapon(player.CurrentWeapon);
                                    player.SetField("perk4bought", 0);
                                }
                                if (player.GetField<int>("perk5bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_rof", true);
                                    player.SetField("perk5bought", 0);
                                }
                                if (player.GetField<int>("perk6bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_stalker", true);
                                    player.SetField("perk6bought", 0);
                                }
                                if (player.GetField<int>("perk7bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_scavenger", true);
                                    player.SetField("perk7bought", 0);
                                }
                                if (player.GetField<int>("perk8bought") == 1)
                                {
                                    player.Call("unsetperk", "specialty_jumpdive", true);
                                    player.SetField("perk8bought", 0);
                                }
                                updatePerkHUD(player, 1);
                                player.Call("iPrintLnBold", string.Format("^1You've lost all of your perks and 200 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 11:
                                //double health
                                player.Call("iPrintLnBold", string.Format("^2Double Health for 30 seconds!"));
                                player.SetField("maxhealth", player.Health + 200);
                                player.Health = (player.Health + 200);
                                AfterDelay(30000, () =>
                                    {
                                        player.Health = 200;
                                        player.SetField("maxhealth", 200);
                                        if (player.GetField<int>("perk1bought") == 1)
                                        {
                                            player.Health = 630;
                                            player.SetField("maxhealth", 630);
                                        }
                                        player.Call("iPrintLnBold", string.Format("^2Double Health over"));
                                    });
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 12:
                                //inf health
                                player.Call("iPrintLnBold", string.Format("^2Infinite Health for 30 seconds!"));
                                player.SetField("maxhealth", player.Health + 999999999);
                                player.Health = player.Health + 999999999;
                                AfterDelay(30000, () =>
                                    {
                                        player.Health = 200;
                                        player.SetField("maxhealth", 200);
                                        if (player.GetField<int>("perk1bought") == 1)
                                        {
                                            player.Health = 630;
                                            player.SetField("maxhealth", 630);
                                        }
                                        player.Call("iPrintLnBold", string.Format("^2Infinite Health over"));
                                    });
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 13:
                                //model1887
                                player.Call("iPrintLnBold", string.Format("^2You've won a Model 1887!"));
                                player.TakeWeapon(player.CurrentWeapon);
                                player.GiveWeapon("iw5_1887_mp");
                                AfterDelay(100, () =>
                                player.SwitchToWeaponImmediate("iw5_1887_mp"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 14:
                                //max ammo
                                player.Call("iPrintLnBold", string.Format("^2You have 1/2 chance for Max Ammo!"));
                                AfterDelay(1500, () =>
                                    {
                                        int? ammo = new Random().Next(2);
                                        switch (ammo)
                                        {
                                            case 0:
                                                player.Call("givemaxammo", player.CurrentWeapon);
                                                player.Call("givemaxammo", "iw5_usp45_mp");
                                                player.Call("givemaxammo", "iw5_p99_mp");
                                                player.Call("givemaxammo", "iw5_mp412_mp");
                                                player.Call("givemaxammo", "iw5_44magnum_mp");
                                                player.Call("givemaxammo", "iw5_deserteagle_mp");
                                                player.Call("givemaxammo", "iw5_fnfiveseven_mp");
                                                player.Call("givemaxammo", "iw5_acr_mp");
                                                player.Call("givemaxammo", "iw5_type95_mp");
                                                player.Call("givemaxammo", "iw5_m4_mp");
                                                player.Call("givemaxammo", "iw5_ak47_mp");
                                                player.Call("givemaxammo", "iw5_m16_mp");
                                                player.Call("givemaxammo", "iw5_mk14_mp");
                                                player.Call("givemaxammo", "iw5_g36c_mp");
                                                player.Call("givemaxammo", "iw5_scar_mp");
                                                player.Call("givemaxammo", "iw5_fad_mp");
                                                player.Call("givemaxammo", "iw5_cm901_mp");
                                                player.Call("givemaxammo", "iw5_mp5_mp");
                                                player.Call("givemaxammo", "iw5_m9_mp");
                                                player.Call("givemaxammo", "iw5_p90_mp");
                                                player.Call("givemaxammo", "iw5_pp90m1_mp");
                                                player.Call("givemaxammo", "iw5_ump45_mp");
                                                player.Call("givemaxammo", "iw5_mp7_mp");
                                                player.Call("givemaxammo", "iw5_fmg9_mp");
                                                player.Call("givemaxammo", "iw5_g18_mp");
                                                player.Call("givemaxammo", "iw5_mp9_mp");
                                                player.Call("givemaxammo", "iw5_skorpion_mp");
                                                player.Call("givemaxammo", "iw5_spas12_mp");
                                                player.Call("givemaxammo", "iw5_aa12_mp");
                                                player.Call("givemaxammo", "iw5_striker_mp");
                                                player.Call("givemaxammo", "iw5_1887_mp");
                                                player.Call("givemaxammo", "iw5_usas12_mp");
                                                player.Call("givemaxammo", "iw5_ksg_mp");
                                                player.Call("givemaxammo", "iw5_m60_mp");
                                                player.Call("givemaxammo", "iw5_m60jugg_mp_eotechlmg_camo07");
                                                player.Call("givemaxammo", "iw5_mk46_mp");
                                                player.Call("givemaxammo", "iw5_pecheneg_mp");
                                                player.Call("givemaxammo", "iw5_sa80_mp");
                                                player.Call("givemaxammo", "iw5_mg36_mp");
                                                player.Call("givemaxammo", "iw5_barrett_mp_barrettscope");
                                                player.Call("givemaxammo", "iw5_msr_mp_msrscope");
                                                player.Call("givemaxammo", "iw5_rsass_mp_rsassscope");
                                                player.Call("givemaxammo", "iw5_dragunov_mp_dragunovscope");
                                                player.Call("givemaxammo", "iw5_as50_mp_as50scope");
                                                player.Call("givemaxammo", "iw5_l96a1_mp_l96a1scope");
                                                player.Call("givemaxammo", "defaultweapon_mp");
                                                player.Call("givemaxammo", "iw5_usp45_mp_akimbo");
                                                player.Call("givemaxammo", "iw5_p99_mp_xmags");
                                                player.Call("givemaxammo", "iw5_mp412jugg_mp");
                                                player.Call("givemaxammo", "iw5_44magnum_mp_xmags");
                                                player.Call("givemaxammo", "iw5_deserteagle_mp_xmags");
                                                player.Call("givemaxammo", "iw5_fnfiveseven_mp_xmags");
                                                player.Call("givemaxammo", "iw5_acr_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_type95_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_m4_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_ak47_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_m16_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_mk14_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_g36c_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_scar_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_fad_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_cm901_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_mp5_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_m9_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_p90_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_pp90m1_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_ump45_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_mp7_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_fmg9_mp_xmags");
                                                player.Call("givemaxammo", "iw5_g18_mp_xmags");
                                                player.Call("givemaxammo", "iw5_mp9_mp_xmags");
                                                player.Call("givemaxammo", "iw5_skorpion_mp_xmags");
                                                player.Call("givemaxammo", "iw5_spas12_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_aa12_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_striker_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_1887_mp_camo09");
                                                player.Call("givemaxammo", "iw5_usas12_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_ksg_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_m60_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_m60jugg_mp_eotechlmg_silencer_camo06");
                                                player.Call("givemaxammo", "iw5_mk46_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_pecheneg_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_sa80_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_mg36_mp_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_barrett_mp_barrettscope_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_msr_mp_msrscope_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_rsass_mp_rsassscope_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_dragunov_mp_dragunovscope_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_as50_mp_as50scope_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_l96a1_mp_l96a1scope_xmags_camo09");
                                                player.Call("givemaxammo", "iw5_usp45_mp_silencer02_akimbo");
                                                player.Call("givemaxammo", "iw5_p99_mp_tactical_xmags");
                                                player.Call("givemaxammo", "iw5_mp412jugg_mp_xmags");
                                                player.Call("givemaxammo", "iw5_44magnum_mp_akimbo_xmags");
                                                player.Call("givemaxammo", "iw5_deserteagle_mp_silencer02_xmags");
                                                player.Call("givemaxammo", "iw5_fnfiveseven_mp_akimbo_xmags");
                                                player.Call("givemaxammo", "iw5_acr_mp_eotech_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_type95_mp_reflex_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_m4_mp_reflex_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_ak47_mp_gp25_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_m16_mp_rof_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_mk14_mp_reflex_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_g36c_mp_hybrid_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_scar_mp_eotech_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_fad_mp_m320_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_cm901_mp_acog_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_mp5_mp_reflexsmg_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_m9_mp_thermalsmg_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_p90_mp_rof_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_pp90m1_mp_silencer_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_ump45_mp_eotechsmg_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_mp7_mp_silencer_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_fmg9_mp_akimbo_xmags");
                                                player.Call("givemaxammo", "iw5_g18_mp_silencer02_xmags");
                                                player.Call("givemaxammo", "iw5_mp9_mp_reflexsmg_xmags");
                                                player.Call("givemaxammo", "iw5_skorpion_mp_akimbo_xmags");
                                                player.Call("givemaxammo", "iw5_spas12_mp_grip_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_aa12_mp_grip_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_striker_mp_grip_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_1887_mp_camo11");
                                                player.Call("givemaxammo", "iw5_usas12_mp_reflex_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_ksg_mp_grip_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_m60_mp_reflexlmg_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_m60jugg_mp_thermal_silencer_camo08");
                                                player.Call("givemaxammo", "iw5_mk46_mp_silencer_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_pecheneg_mp_thermal_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_sa80_mp_reflexlmg_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_mg36_mp_grip_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_barrett_mp_barrettscope_acog_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_msr_mp_msrscopevz_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_rsass_mp_rsassscope_thermal_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_dragunov_mp_dragunovscope_acog_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_as50_mp_as50scopevz_xmags_camo11");
                                                player.Call("givemaxammo", "iw5_l96a1_mp_l96a1scopevz_xmags_camo11");
                                                player.Call("givemaxammo", "xm25_mp");
                                                player.Call("givemaxammo", "gl_mp");
                                                player.Call("givemaxammo", "rpg_mp");
                                                player.Call("givemaxammo", "iw5_smaw_mp");
                                                player.Call("givemaxammo", "stinger_mp");
                                                player.Call("givemaxammo", "m320_mp");
                                                player.Call("iPrintLnBold", string.Format("^2You've won the Max Ammo!"));
                                                player.Call("playlocalsound", "ammo_crate_use");
                                                player.SetField("GamblerInUse", 0);
                                                break;
                                            case 1:
                                                player.Call("iPrintLnBold", string.Format("^1No Max Ammo."));
                                                player.SetField("GamblerInUse", 0);
                                                break;
                                        }
                                    });
                                AfterDelay(121500, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 15:
                                //lose 1000
                                player.SetField("cash", player.GetField<int>("cash") - 1000);
                                foreach (Entity players in Players)
                                ScorePopup(players, 1000, "R", player.EntRef);
                                if (player.GetField<int>("cash") < 0)
                                    player.SetField("cash", 0);
                                player.Call("iPrintLnBold", string.Format("^1You've lost 1000 points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 16:
                                //lose all $
                                foreach (Entity players in Players)
                                ScorePopup(players, player.GetField<int>("cash"), "R", player.EntRef);
                                player.SetField("cash", 0);
                                player.Call("iPrintLnBold", string.Format("^1You've lost all of your points!"));
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                            case 17:
                                //live or die
                                player.Call("iPrintLnBold", string.Format("^2God decides if you live or die in 5 seconds"));
                AfterDelay(1500, () =>
                {
                     player.Call("iPrintLnBold", string.Format("^24"));
                });
                AfterDelay(2500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^23"));
                });
                AfterDelay(3500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^22"));
                });
                AfterDelay(4500, () =>
                {
                    player.Call("iPrintLnBold", string.Format("^21"));
                });
                AfterDelay(5500, () =>
                {
                          int? death = new Random().Next(4);
                          switch (death)
                          {
                              case 0:
                                  player.Call("suicide");
                                  break;
                              case 1:
                                  player.Call("iPrintLnBold", string.Format("^2You live."));
                                  break;
                              case 2:
                                  player.Call("iPrintLnBold", string.Format("^2You live."));
                                  break;
                              case 3:
                                  player.Call("iPrintLnBold", string.Format("^2You live."));
                                  break;
                          }
                });
                                player.SetField("GamblerInUse", 0);
                                AfterDelay(120000, () =>
                                    player.SetField("GamblerReady", 1));
                                break;
                        }
                    });
            }
        }

        public void usedPerk1(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 2500) return;
            if (player.GetField<int>("perk1bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 2500);
                foreach (Entity players in Players)
                ScorePopup(players, 2500, "R", player.EntRef);
                player.SetPerk("specialty_armorvest", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("maxhealth", 500);
                player.Health = 500;
                player.SetField("perk1bought", 1);
                player.SetField("PerkBought", "cardicon_juggernaut_1");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Juggernaut", "cardicon_juggernaut_1", 0);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk2(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 2000) return;
            if (player.GetField<int>("perk2bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 2000);
                foreach (Entity players in Players)
                ScorePopup(players, 2000, "R", player.EntRef);
                player.SetPerk("specialty_lightweight", true, true);
                player.SetPerk("specialty_marathon", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk2bought", 1);
                player.SetField("PerkBought", "specialty_longersprint");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Stamin-Up", "specialty_longersprint", 1);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk3(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 3000) return;
            if (player.GetField<int>("perk3bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 3000);
                foreach (Entity players in Players)
                ScorePopup(players, 3000, "R", player.EntRef);
                player.SetPerk("specialty_fastreload", true, true);
                player.SetPerk("specialty_quickswap", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk3bought", 1);
                player.SetField("PerkBought", "specialty_fastreload");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Speed Cola", "specialty_fastreload", 2);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk4(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 4000) return;
            if (player.GetField<int>("perk4bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 4000);
                foreach (Entity players in Players)
                ScorePopup(players, 4000, "R", player.EntRef);
                player.SetField("NewGunReady", 1);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk4bought", 1);
                player.SetField("PerkBought", "specialty_twoprimaries");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Mule Kick", "specialty_twoprimaries", 3);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk5(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 2000) return;
            if (player.GetField<int>("perk5bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 2000);
                foreach (Entity players in Players)
                ScorePopup(players, 2000, "R", player.EntRef);
                player.SetPerk("specialty_rof", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk5bought", 1);
                player.SetField("PerkBought", "cardicon_gears");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Double Tap", "cardicon_gears", 4);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk6(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 1500) return;
            if (player.GetField<int>("perk6bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 1500);
                foreach (Entity players in Players)
                ScorePopup(players, 1500, "R", player.EntRef);
                player.SetPerk("specialty_stalker", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk6bought", 1);
                player.SetField("PerkBought", "specialty_stalker");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Stalker Soda", "specialty_stalker", 5);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk7(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 2000) return;
            if (player.GetField<int>("perk7bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 3000);
                foreach (Entity players in Players)
                ScorePopup(players, 2000, "R", player.EntRef);
                player.SetPerk("specialty_scavenger", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk7bought", 1);
                player.SetField("PerkBought", "specialty_scavenger");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Vulture-Aid", "specialty_scavenger", 6);
                updatePerkHUD(player, 0);
            }
        }

        public void usedPerk8(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.GetField<int>("cash") < 1000) return;
            if (player.GetField<int>("perk8bought").Equals(1)) return;
            else
            {
                player.SetField("cash", player.GetField<int>("cash") - 1000);
                foreach (Entity players in Players)
                ScorePopup(players, 1000, "R", player.EntRef);
                player.SetPerk("specialty_jumpdive", true, true);
                player.Call("setblurforplayer", 10, 0.3f);
                AfterDelay(700, () =>
                    player.Call("setblurforplayer", 0, 0.3f));
                player.SetField("perk8bought", 1);
                player.SetField("PerkBought", "cardicon_dive");
                player.SetField("perkHUDReady", 1);
                player.Call("playlocalsound", "earn_perk");
                ShowBoughtPerk(player, "Diver's Splash", "cardicon_dive", 7);
                updatePerkHUD(player, 0);
            }
        }

        public void usedWallWeapon(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            //if (player.CurrentWeapon == "claymore_mp") return;
            int price = box.GetField<int>("price");
            if (player.GetField<int>("cash") < price) return;
            if (player.IsAlive)
            {
                player.SetField("cash", player.GetField<int>("cash") - price);
                foreach (Entity players in Players)
                ScorePopup(players, price, "R", player.EntRef);
                string weapon = box.GetField<string>("wep");
                player.GiveWeapon(weapon);
                player.SwitchToWeaponImmediate(weapon);
                player.Call("playsoundtoplayer", "oldschool_pickup");
            }
        }

        public void usedClaymore(Entity box, Entity player)
        {
            if (player.GetField<string>("sessionteam") == "axis") return;
            if (player.CurrentWeapon == "claymore_mp") return;
            if (player.GetField<int>("cash") < 500) return;
            if (player.IsAlive)
            {
                player.SetField("cash", player.GetField<int>("cash") - 500);
                foreach (Entity players in Players)
                    ScorePopup(players, 500, "R", player.EntRef);
                player.GiveWeapon("claymore_mp");
                player.SwitchToWeaponImmediate("claymore_mp");
                //player.Call("playsoundtoplayer", "mp_oldschool_pickup_01");
            }
        }

        private void usedDoor(Entity door, Entity player)
        {
            if (!player.IsAlive) return;
            if (player.GetField<string>("sessionteam") != "axis")
            {
                if (door.GetField<int>("doorType") == 1)
                {
                    if (player.GetField<int>("cash") < 500) return;
                    player.SetField("cash", player.GetField<int>("cash") - 500);
                    foreach (Entity players in Players)
                    ScorePopup(players, 500, "R", player.EntRef);
                }
                else if (door.GetField<int>("doorType") == 2)
                {
                    if (player.GetField<int>("cash") < 750) return;
                    player.SetField("cash", player.GetField<int>("cash") - 750);
                    foreach (Entity players in Players)
                    ScorePopup(players, 750, "R", player.EntRef);
                }
                else if (door.GetField<int>("doorType") == 3)
                {
                    if (player.GetField<int>("cash") < 1000) return;
                    player.SetField("cash", player.GetField<int>("cash") - 1000);
                    foreach (Entity players in Players)
                    ScorePopup(players, 1000, "R", player.EntRef);
                }
                else if (door.GetField<int>("doorType") == 4)
                {
                    if (player.GetField<int>("cash") < 1500) return;
                    player.SetField("cash", player.GetField<int>("cash") - 1500);
                    foreach (Entity players in Players)
                    ScorePopup(players, 1500, "R", player.EntRef);
                }
                        door.Call(33399, new Parameter(door.GetField<Vector3>("open")), 1); // moveto
                        door.SetField("state", "open");
            }
        }

        public void MakeUsable(Entity ent, string type, int range)
        {
            ent.SetField("usabletype", type);
            ent.SetField("range", range);
            usables.Add(ent);
        }

        public void ShowBoughtPerk(Entity player, string Name, string ImageName, int index)
        {
            HudElem Desc = HudElem.CreateFontString(player, "hudsmall", 1.5f);
            Desc.SetText(PerkDescs[index]);
            Desc.SetPoint("CENTER", "CENTER", 0, -100);
            Desc.Color = new Vector3(0.99f, 1, 0.8f);
            Desc.HideWhenInMenu = true;
            Desc.Alpha = 0;
            HudElem PerkName = HudElem.CreateFontString(player, "hudsmall", 1.7f);
            PerkName.SetText(Name);
            PerkName.SetPoint("CENTER", "CENTER", 0, -170);
            PerkName.Color = new Vector3(0.99f, 1, 0.8f);
            PerkName.HideWhenInMenu = true;
            PerkName.Alpha = 0;
            HudElem Image = HudElem.NewClientHudElem(player);
            Image.SetShader(ImageName, 50, 50);
            Image.X = 0;
            Image.Y = 83;
            Image.AlignX = "CENTER";
            Image.AlignY = "CENTER";
            Image.HorzAlign = "CENTER";
            Image.VertAlign = "CENTER";
            Image.HideWhenInMenu = true;
            Image.Alpha = 0;
            int ImageX = GetPerkPath(player);
            AfterDelay(1000, () =>
                {
                    Desc.Call("fadeovertime", 0.6f);
                    PerkName.Call("fadeovertime", 0.6f);
                    Image.Call("fadeovertime", 0.6f);
                    Desc.Alpha = 1;
                    PerkName.Alpha = 1;
                    Image.Alpha = 1;
                    AfterDelay(5000, () =>
                        {
                            Desc.Call("fadeovertime", 0.6f);
                            Desc.Alpha = 0;
                            PerkName.Call("fadeovertime", 0.6f);
                            PerkName.Alpha = 0;
                            Image.Call("scaleovertime", 3, 30, 30);
                            Image.Call("moveovertime", 2.9f);
                            //Image.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", GetPerkPath(player), -54);
                            Image.X = ImageX;
                            Image.Y = 444;
                            AfterDelay(3050, () =>
                                {
                                    Image.Call("destroy");
                                    Desc.Call("destroy");
                                    PerkName.Call("destroy");
                                });
                        });
                });
        }

        public string getDoorText(Entity door, Entity player)
        {
            //int cost = door.GetField<int>("doorcost");
            if (player.GetField<string>("sessionteam") == "allies" && door.GetField<string>("state") == "close" && door.GetField<int>("doorType") == 1)
                return "Hold ^3[{+activate}] ^7to open the door [Cost: 500]";
            else if (player.GetField<string>("sessionteam") == "allies" && door.GetField<string>("state") == "close" && door.GetField<int>("doorType") == 2)
                return "Hold ^3[{+activate}] ^7to open the door [Cost: 750]";
            else if (player.GetField<string>("sessionteam") == "allies" && door.GetField<string>("state") == "close" && door.GetField<int>("doorType") == 3)
                return "Hold ^3[{+activate}] ^7to open the door [Cost: 1000]";
            else if (player.GetField<string>("sessionteam") == "allies" && door.GetField<string>("state") == "close" && door.GetField<int>("doorType") == 4)
                return "Hold ^3[{+activate}] ^7to open the door [Cost: 1500]";
            return "";
        }

        public void CreateDoor(Vector3 open, Vector3 close, Vector3 angle, int size, int height, int range, int type)
        {
            double offset = (((size / 2) - 0.5) * -1);
            Entity center = Call<Entity>("spawn", "script_model", new Parameter(close));
            for (int j = 0; j < size; j++)
            {
                Entity door = spawnCrate(close + (new Vector3(0, 30, 0) * (float)offset), new Vector3(0, 0, 0), false, false);
                door.Call("setModel", "com_plasticcase_enemy");
                door.Call("enablelinkto");
                door.Call("linkto", center);
                for (int h = 1; h < height; h++)
                {
                    Entity door2 = spawnCrate(close + (new Vector3(0, 30, 0) * (float)offset) - (new Vector3(70, 0, 0) * h), new Vector3(0, 0, 0), false, false);
                    door2.Call("setModel", "com_plasticcase_enemy");
                    door2.Call("enablelinkto");
                    door2.Call("linkto", center);
                }
                offset += 1;
            }
            center.SetField("angles", new Parameter(angle));
            center.SetField("open", new Parameter(open));
            center.SetField("close", new Parameter(close));
            center.SetField("doorType", type);

            MakeUsable(center, "door", range);
            //center.Call(33529, new Parameter(center.GetField<Vector3>("close"))); // moveto
            center.SetField("state", "close");
        }

        public Entity CreateWall(Vector3 start, Vector3 end)
        {
            float D = new Vector3(start.X, start.Y, 0).DistanceTo(new Vector3(end.X, end.Y, 0));
            float H = new Vector3(0, 0, start.Z).DistanceTo(new Vector3(0, 0, end.Z));
            int blocks = (int)Math.Round(D / 55, 0);
            int height = (int)Math.Round(H / 30, 0);

            Vector3 C = end - start;
            Vector3 A = new Vector3(C.X / blocks, C.Y / blocks, C.Z / height);
            float TXA = A.X / 4;
            float TYA = A.Y / 4;
            Vector3 angle = Call<Vector3>("vectortoangles", new Parameter(C));
            angle = new Vector3(0, angle.Y, 90);
            Entity center = Call<Entity>("spawn", "script_origin", new Parameter(new Vector3(
                (start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2)));
            for (int h = 0; h < height; h++)
            {
                Entity crate = spawnCrate((start + new Vector3(TXA, TYA, 10) + (new Vector3(0, 0, A.Z) * h)), angle, false, false);
                crate.Call("enablelinkto");
                crate.Call("linkto", center);
                for (int i = 0; i < blocks; i++)
                {
                    crate = spawnCrate(start + (new Vector3(A.X, A.Y, 0) * i) + new Vector3(0, 0, 10) + (new Vector3(0, 0, A.Z) * h), angle, false, false);
                    crate.Call("enablelinkto");
                    crate.Call("linkto", center);
                }
                crate = spawnCrate(new Vector3(end.X, end.Y, start.Z) + new Vector3(TXA * -1, TYA * -1, 10) + (new Vector3(0, 0, A.Z) * h), angle, false, false);
                crate.Call("enablelinkto");
                crate.Call("linkto", center);
            }
            return center;
        }
        public Entity CreateInvisibleWall(Vector3 start, Vector3 end)
        {
            float D = new Vector3(start.X, start.Y, 0).DistanceTo(new Vector3(end.X, end.Y, 0));
            float H = new Vector3(0, 0, start.Z).DistanceTo(new Vector3(0, 0, end.Z));
            int blocks = (int)Math.Round(D / 55, 0);
            int height = (int)Math.Round(H / 30, 0);

            Vector3 C = end - start;
            Vector3 A = new Vector3(C.X / blocks, C.Y / blocks, C.Z / height);
            float TXA = A.X / 4;
            float TYA = A.Y / 4;
            Vector3 angle = Call<Vector3>("vectortoangles", new Parameter(C));
            angle = new Vector3(0, angle.Y, 90);
            Entity center = Call<Entity>("spawn", "script_origin", new Parameter(new Vector3(
                (start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2)));
            for (int h = 0; h < height; h++)
            {
                Entity crate = spawnCrate((start + new Vector3(TXA, TYA, 10) + (new Vector3(0, 0, A.Z) * h)), angle, true, false);
                crate.Call("enablelinkto");
                crate.Call("linkto", center);
                for (int i = 0; i < blocks; i++)
                {
                    crate = spawnCrate(start + (new Vector3(A.X, A.Y, 0) * i) + new Vector3(0, 0, 10) + (new Vector3(0, 0, A.Z) * h), angle, true, false);
                    crate.Call("enablelinkto");
                    crate.Call("linkto", center);
                }
                crate = spawnCrate(new Vector3(end.X, end.Y, start.Z) + new Vector3(TXA * -1, TYA * -1, 10) + (new Vector3(0, 0, A.Z) * h), angle, true, false);
                crate.Call("enablelinkto");
                crate.Call("linkto", center);
            }
            return center;
        }
        public Entity CreateDeathWall(Vector3 start, Vector3 end)
        {
            float D = new Vector3(start.X, start.Y, 0).DistanceTo(new Vector3(end.X, end.Y, 0));
            float H = new Vector3(0, 0, start.Z).DistanceTo(new Vector3(0, 0, end.Z));
            int blocks = (int)Math.Round(D / 55, 0);
            int height = (int)Math.Round(H / 30, 0);

            Vector3 C = end - start;
            Vector3 A = new Vector3(C.X / blocks, C.Y / blocks, C.Z / height);
            float TXA = A.X / 4;
            float TYA = A.Y / 4;
            Vector3 angle = Call<Vector3>("vectortoangles", new Parameter(C));
            angle = new Vector3(0, angle.Y, 90);
            Entity center = Call<Entity>("spawn", "script_origin", new Parameter(new Vector3(
                (start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2)));
            for (int h = 0; h < height; h++)
            {
                Entity crate = spawnCrate((start + new Vector3(TXA, TYA, 10) + (new Vector3(0, 0, A.Z) * h)), angle, true, true);
                crate.Call("enablelinkto");
                crate.Call("linkto", center);
                for (int i = 0; i < blocks; i++)
                {
                    crate = spawnCrate(start + (new Vector3(A.X, A.Y, 0) * i) + new Vector3(0, 0, 10) + (new Vector3(0, 0, A.Z) * h), angle, true, true);
                    crate.Call("enablelinkto");
                    crate.Call("linkto", center);
                    SetDeathWall(crate);
                }
                crate = spawnCrate(new Vector3(end.X, end.Y, start.Z) + new Vector3(TXA * -1, TYA * -1, 10) + (new Vector3(0, 0, A.Z) * h), angle, true, true);
                crate.Call("enablelinkto");
                crate.Call("linkto", center);
            }
            return center;
        }
        public void SetDeathWall(Entity crate)
        {
            OnInterval(200, () =>
                {
                    foreach (Entity player in Players)
                    {
                        if (player.Origin.DistanceTo(crate.Origin) < 60 && player.GetField<string>("sessionteam") == "allies")
                        {
                            player.Call("suicide");
                        }
                    }
                    return true;
                });
        }
        public Entity CreateFloor(Vector3 corner1, Vector3 corner2)
        {
            float width = corner1.X - corner2.X;
            if (width < 0) width = width * -1;
            float length = corner1.Y - corner2.Y;
            if (length < 0) length = length * -1;

            int bwide = (int)Math.Round(width / 50, 0);
            int blength = (int)Math.Round(length / 30, 0);
            Vector3 C = corner2 - corner1;
            Vector3 A = new Vector3(C.X / bwide, C.Y / blength, 0);
            Entity center = Call<Entity>("spawn", "script_origin", new Parameter(new Vector3(
                (corner1.X + corner2.X) / 2, (corner1.Y + corner2.Y) / 2, corner1.Z)));
            for (int i = 0; i < bwide; i++)
            {
                for (int j = 0; j < blength; j++)
                {
                    Entity crate = spawnCrate(corner1 + (new Vector3(A.X, 0, 0) * i) + (new Vector3(0, A.Y, 0) * j), new Vector3(0, 0, 0), false, false);
                    crate.Call("enablelinkto");
                    crate.Call("linkto", center);
                }
            }
            return center;
        }

        private int _flagCount = 0;

        public void CreateElevator(Vector3 enter, Vector3 exit)
        {
            Entity flag = Call<Entity>("spawn", "script_model", new Parameter(enter));
            flag.Call("setModel", getAlliesFlagModel(_mapname));
            Entity flag2 = Call<Entity>("spawn", "script_model", new Parameter(exit));
            flag2.Call("setModel", getAxisFlagModel(_mapname));

            OnInterval(100, () =>
            {
                foreach (Entity player in Players)
                {
                    if (player.Origin.DistanceTo(enter) <= 50)
                    {
                        player.Call("setorigin", new Parameter(exit));
                    }
                }
                return true;
            });
        }

        public Entity spawnModel(string model, Vector3 origin, Vector3 angles)
        {
            Entity ent = Call<Entity>("spawn", "script_model", new Parameter(origin));
            ent.Call("setmodel", model);
            ent.SetField("angles", new Parameter(angles));
            return ent;
        }

        public Entity spawnCrate(Vector3 origin, Vector3 angles, bool Invisible, bool Death)
        {
            Entity ent = Call<Entity>("spawn", "script_model", new Parameter(origin));
            if (!Invisible && !Death) ent.Call("setmodel", "com_plasticcase_friendly");
            ent.SetField("angles", new Parameter(angles));
            if (!Death) ent.Call(33353, _airdropCollision); // clonebrushmodeltoscriptmodel
            return ent;
        }
        public void InitJuggAnim(Entity crate, Entity rebar)
        {
                        foreach (Entity player in Players)
                        {
                            player.Call(33466, "nuke_wave");
                        }
                        AfterDelay(2800, () =>
                            {
                                Entity RockCrumble = Call<Entity>("spawnfx", RockFX, rebar.Origin);
                                Call("triggerfx", RockCrumble);
                                Entity Smoke2 = Call<Entity>("spawnfx", CrateFX, rebar.Origin + new Vector3(100, 0, 0));
                                Call("triggerfx", Smoke2);
                                Entity Smoke3 = Call<Entity>("spawnfx", CrateFX, rebar.Origin - new Vector3(100, 0, 0));
                                Call("triggerfx", Smoke3);
                                Call("earthquake", 0.5f, 6.5f, rebar.Origin - new Vector3(0, 0, 500), 5000);
                                AfterDelay(2000, () =>
                                    {
                                        rebar.Call(32915, "talon_destroyed");
                                        rebar.Call("rotateto", new Vector3(50, 0, -25), 4, 0.5f, 1);
                                        rebar.Call("moveto", rebar.Origin - new Vector3(0, 0, 50), 4, 0.5f, 1);
                                        AfterDelay(1000, () =>
                                            {
                                                Vector3 dropImpulse = new Vector3(300, 50, -60);
                                                crate.Call(33351, new Vector3(0, 0, 0), dropImpulse);
                                                AfterDelay(1500, () =>
                                                    {
                                                        Entity CrateSmoke = Call<Entity>("spawnfx", CrateFX, crate.Origin);
                                                        Call("triggerfx", CrateSmoke);
                                                        AfterDelay(3000, () =>
                                                            {
                                                                CrateSmoke.Call("delete");
                                                                RockCrumble.Call("delete");
                                                                Smoke2.Call("delete");
                                                                Smoke3.Call("delete");
                                                                AfterDelay(1000, () =>
                                                                    MakeUsable(crate, "perk1", 75));
                                                            });
                                                    });
                                            });
                                    });
                            });
        }
        /*
        public Entity[] getSpawns(string name)
        {
            return Call<Entity[]>("getentarray", name, "classname");
        }
        public void removeSpawn(Entity spawn)
        {
            spawn.Call("delete");
        }
        public void createSpawn(string type, Vector3 origin, Vector3 angle)
        {
            Entity spawn = Call<Entity>("spawn", type, new Parameter(origin));
            spawn.SetField("angles", new Parameter(angle));
        }
        possibly implement this in a later version to make spawns definite */

        public string PapWeapon(Entity ent)
        {
                string weapon = ent.CurrentWeapon;
                if (weapon == "iw5_scar_mp") return "iw5_scar_mp_eotech_xmags_camo11";
                else if (weapon == "iw5_mp5_mp") return "iw5_mp5_mp_reflexsmg_xmags_camo11";
                else if (weapon == "iw5_ak47_mp") return "iw5_ak47_mp_gp25_xmags_camo11";
                else if (weapon == "iw5_m60jugg_mp_eotechlmg_camo07") return "iw5_m60jugg_mp_thermal_silencer_camo08";
                else if (weapon == "iw5_mp412_mp") return "iw5_mp412jugg_mp_xmags";
                else if (weapon == "iw5_deserteagle_mp") return "iw5_deserteagle_mp_xmags_silencer02";
                else if (weapon == "iw5_usp45_mp") return "iw5_usp45_mp_akimbo_silencer02";
                else if (weapon == "iw5_p90_mp") return "iw5_p90_mp_rof_xmags_camo11";
                else if (weapon == "iw5_m60_mp") return "iw5_m60jugg_mp_reflexlmg_xmags";
                else if (weapon == "iw5_as50_mp_as50scope") return "iw5_as50_mp_as50scopevz_xmags_camo11";
                else if (weapon == "iw5_msr_mp_msrscope") return "iw5_msr_mp_msrscopevz_xmags_camo11";
                else if (weapon == "iw5_aa12_mp") return "iw5_aa12_mp_xmags_grip_camo11";
                else if (weapon == "iw5_1887_mp") return "iw5_1887_mp_camo11";
                else if (weapon == "iw5_skorpion_mp") return "iw5_skorpion_mp_xmags_akimbo";
                else if (weapon == "iw5_mp9_mp") return "iw5_mp9_mp_xmags_reflexsmg";
                else if (weapon == "iw5_p99_mp") return "iw5_p99_mp_tactical_xmags";
                else if (weapon == "iw5_fnfiveseven_mp") return "iw5_fnfiveseven_mp_xmags_akimbo";
                else if (weapon == "iw5_44magnum_mp") return "iw5_44magnum_mp_xmags_akimbo";
                else if (weapon == "iw5_fmg9_mp") return "iw5_fmg9_mp_xmags_akimbo";
                else if (weapon == "iw5_g18_mp") return "iw5_g18_mp_xmags_silencer02";
                else if (weapon == "iw5_smaw_mp") return "rpg_mp";
                else if (weapon == "xm25_mp") return "uav_strike_marker_mp";
                else if (weapon == "m320_mp") return "gl_mp";
                else if (weapon == "iw5_m4_mp") return "iw5_m4_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_m16_mp") return "iw5_m16_mp_xmags_rof_camo11";
                else if (weapon == "iw5_cm901_mp") return "iw5_cm901_mp_xmags_acog_camo11";
                else if (weapon == "iw5_type95_mp") return "iw5_type95_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_acr_mp") return "iw5_acr_mp_xmags_eotech_camo11";
                else if (weapon == "iw5_mk14_mp") return "iw5_mk14_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_g36c_mp") return "iw5_g36c_mp_xmags_hybrid_camo11";
                else if (weapon == "iw5_fad_mp") return "iw5_fad_mp_xmags_m320_camo11";
                else if (weapon == "iw5_ump45_mp") return "iw5_ump45_mp_xmags_eotechsmg_camo11";
                else if (weapon == "iw5_pp90m1_mp") return "iw5_pp90m1_mp_xmags_silencer_camo11";
                else if (weapon == "iw5_m9_mp") return "iw5_m9_mp_xmags_thermalsmg_camo11";
                else if (weapon == "iw5_mp7_mp") return "iw5_mp7_mp_xmags_silencer_camo11";
                else if (weapon == "iw5_dragunov_mp_dragunovscope") return "iw5_dragunov_mp_acog_xmags_camo11";
                else if (weapon == "iw5_barrett_mp_barrettscope") return "iw5_barrett_mp_acog_xmags_camo11";
                else if (weapon == "iw5_l96a1_mp_l96a1scope") return "iw5_l96a1_mp_l96a1scopevz_xmags_camo11";
                else if (weapon == "iw5_rsass_mp_rsassscope") return "iw5_rsass_mp_thermal_xmags_camo11";
                else if (weapon == "iw5_sa80_mp") return "iw5_sa80_mp_reflexlmg_xmags_camo11";
                else if (weapon == "iw5_mg36_mp") return "iw5_mg36_mp_xmags_grip_camo11";
                else if (weapon == "iw5_pecheneg_mp") return "iw5_pecheneg_mp_xmags_thermal_camo11";
                else if (weapon == "iw5_mk46_mp") return "iw5_mk46_mp_xmags_silencer_camo11";
                else if (weapon == "iw5_usas12_mp") return "iw5_usas12_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_ksg_mp") return "iw5_ksg_mp_grip_xmags_camo11";
                else if (weapon == "iw5_spas12_mp") return "iw5_spas12_mp_grip_xmags_camo11";
                else if (weapon == "iw5_striker_mp") return "iw5_striker_mp_xmags_grip_camo11";
                else if (weapon == "iw5_skorpion_mp_eotechsmg") return "iw5_skorpion_mp_eotechsmg_xmags";
                else if (weapon == "riotshield_mp") return "iw5_riotshieldjugg_mp";
                else if (weapon == "scrambler_mp") return "iw5_riotshieldjugg_mp";
                else return "";
        }

        public string UpgradeWeapon(Entity ent)
        {
                string weapon = ent.CurrentWeapon;
                if (weapon == "iw5_scar_mp") return "iw5_scar_mp_xmags_camo09";
                else if (weapon == "iw5_mp5_mp") return "iw5_mp5_mp_xmags_camo09";
                else if (weapon == "iw5_ak47_mp") return "iw5_ak47_mp_xmags_camo09";
                else if (weapon == "iw5_m60jugg_mp_eotechlmg_camo07") return "iw5_m60jugg_mp_eotechlmg_silencer_camo06";
                else if (weapon == "iw5_mp412_mp") return "iw5_mp412jugg_mp";
                else if (weapon == "iw5_deserteagle_mp") return "iw5_deserteagle_mp_xmags";
                else if (weapon == "iw5_usp45_mp") return "iw5_usp45_mp_akimbo";
                else if (weapon == "iw5_p90_mp") return "iw5_p90_mp_xmags_camo09";
                else if (weapon == "iw5_m60_mp") return "iw5_m60_mp_xmags_camo09";
                else if (weapon == "iw5_as50_mp_as50scope") return "iw5_as50_mp_as50scope_xmags_camo09";
                else if (weapon == "iw5_msr_mp_msrscope") return "iw5_msr_mp_msrscope_xmags_camo09";
                else if (weapon == "iw5_aa12_mp") return "iw5_aa12_mp_xmags_camo09";
                else if (weapon == "iw5_1887_mp") return "iw5_1887_mp_camo09";
                else if (weapon == "iw5_skorpion_mp") return "iw5_skorpion_mp_xmags";
                else if (weapon == "iw5_mp9_mp") return "iw5_mp9_mp_xmags";
                else if (weapon == "iw5_p99_mp") return "iw5_p99_mp_xmags";
                else if (weapon == "iw5_fnfiveseven_mp") return "iw5_fnfiveseven_mp_xmags";
                else if (weapon == "iw5_44magnum_mp") return "iw5_44magnum_mp_xmags";
                else if (weapon == "iw5_fmg9_mp") return "iw5_fmg9_mp_xmags";
                else if (weapon == "iw5_g18_mp") return "iw5_g18_mp_xmags";
                else if (weapon == "iw5_m4_mp") return "iw5_m4_mp_xmags_camo09";
                else if (weapon == "iw5_m16_mp") return "iw5_m16_mp_xmags_camo09";
                else if (weapon == "iw5_cm901_mp") return "iw5_cm901_mp_xmags_camo09";
                else if (weapon == "iw5_type95_mp") return "iw5_type95_mp_xmags_camo09";
                else if (weapon == "iw5_acr_mp") return "iw5_acr_mp_xmags_camo09";
                else if (weapon == "iw5_mk14_mp") return "iw5_mk14_mp_xmags_camo09";
                else if (weapon == "iw5_g36c_mp") return "iw5_g36c_mp_xmags_camo09";
                else if (weapon == "iw5_fad_mp") return "iw5_fad_mp_xmags_camo09";
                else if (weapon == "iw5_ump45_mp") return "iw5_ump45_mp_xmags_camo09";
                else if (weapon == "iw5_pp90m1_mp") return "iw5_pp90m1_mp_xmags_camo09";
                else if (weapon == "iw5_m9_mp") return "iw5_m9_mp_xmags_camo09";
                else if (weapon == "iw5_mp7_mp") return "iw5_mp7_mp_xmags_camo09";
                else if (weapon == "iw5_dragunov_mp_dragunovscope") return "iw5_dragunov_mp_dragunovscope_xmags_camo09";
                else if (weapon == "iw5_barrett_mp_barrettscope") return "iw5_barrett_mp_barrettscope_xmags_camo09";
                else if (weapon == "iw5_l96a1_mp_l96a1scope") return "iw5_l96a1_mp_l96a1scope_xmags_camo09";
                else if (weapon == "iw5_rsass_mp_rsassscope") return "iw5_rsass_mp_rsassscope_xmags_camo09";
                else if (weapon == "iw5_sa80_mp") return "iw5_sa80_mp_xmags_camo09";
                else if (weapon == "iw5_mg36_mp") return "iw5_mg36_mp_xmags_camo09";
                else if (weapon == "iw5_pecheneg_mp") return "iw5_pecheneg_mp_xmags_camo09";
                else if (weapon == "iw5_mk46_mp") return "iw5_mk46_mp_xmags_camo09";
                else if (weapon == "iw5_usas12_mp") return "iw5_usas12_mp_xmags_camo09";
                else if (weapon == "iw5_ksg_mp") return "iw5_ksg_mp_xmags_camo09";
                else if (weapon == "iw5_spas12_mp") return "iw5_spas12_mp_xmags_camo09";
                else if (weapon == "iw5_striker_mp") return "iw5_striker_mp_xmags_camo09";
                else if (weapon == "scrambler_mp") return "riotshield_mp";
                else if (weapon == "iw5_scar_mp_xmags_camo09") return "iw5_scar_mp_eotech_xmags_camo11";
                else if (weapon == "iw5_mp5_mp_xmags_camo09") return "iw5_mp5_mp_reflexsmg_xmags_camo11";
                else if (weapon == "iw5_ak47_mp_xmags_camo09") return "iw5_ak47_mp_gp25_xmags_camo11";
                else if (weapon == "iw5_m60jugg_mp_eotechlmg_silencer_camo06") return "iw5_m60jugg_mp_silencer_thermal_camo08";
                else if (weapon == "iw5_mp412jugg_mp") return "iw5_mp412jugg_mp_xmags";
                else if (weapon == "iw5_deserteagle_mp_xmags") return "iw5_deserteagle_mp_xmags_silencer02";
                else if (weapon == "iw5_usp45_mp_akimbo") return "iw5_usp45_mp_akimbo_silencer02";
                else if (weapon == "iw5_p90_mp_xmags_camo09") return "iw5_p90_mp_rof_xmags_camo11";
                else if (weapon == "iw5_m60_mp_xmags_camo09") return "iw5_m60jugg_mp_reflexlmg_xmags";
                else if (weapon == "iw5_as50_mp_as50scope_xmags_camo09") return "iw5_as50_mp_as50scopevz_xmags_camo11";
                else if (weapon == "iw5_msr_mp_msrscope_xmags_camo09") return "iw5_msr_mp_msrscopevz_xmags_camo11";
                else if (weapon == "iw5_aa12_mp_xmags_camo09") return "iw5_aa12_mp_xmags_grip_camo11";
                else if (weapon == "iw5_1887_mp_camo09") return "iw5_1887_mp_camo11";
                else if (weapon == "iw5_skorpion_mp_xmags") return "iw5_skorpion_mp_xmags_akimbo";
                else if (weapon == "iw5_mp9_mp_xmags") return "iw5_mp9_mp_xmags_reflexsmg";
                else if (weapon == "iw5_p99_mp_xmags") return "iw5_p99_mp_tactical_xmags";
                else if (weapon == "iw5_fnfiveseven_mp_xmags") return "iw5_fnfiveseven_mp_xmags_akimbo";
                else if (weapon == "iw5_44magnum_mp_xmags") return "iw5_44magnum_mp_xmags_akimbo";
                else if (weapon == "iw5_fmg9_mp_xmags") return "iw5_fmg9_mp_xmags_akimbo";
                else if (weapon == "iw5_g18_mp_xmags") return "iw5_g18_mp_xmags_silencer02";
                else if (weapon == "iw5_m4_mp_xmags_camo09") return "iw5_m4_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_m16_mp_xmags_camo09") return "iw5_m16_mp_xmags_rof_camo11";
                else if (weapon == "iw5_cm901_mp_xmags_camo09") return "iw5_cm901_mp_xmags_acog_camo11";
                else if (weapon == "iw5_type95_mp_xmags_camo09") return "iw5_type95_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_acr_mp_xmags_camo09") return "iw5_acr_mp_xmags_eotech_camo11";
                else if (weapon == "iw5_mk14_mp_xmags_camo09") return "iw5_mk14_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_g36c_mp_xmags_camo09") return "iw5_g36c_mp_xmags_hybrid_camo11";
                else if (weapon == "iw5_fad_mp_xmags_camo09") return "iw5_fad_mp_xmags_m320_camo11";
                else if (weapon == "iw5_ump45_mp_xmags_camo09") return "iw5_ump45_mp_xmags_eotechsmg_camo11";
                else if (weapon == "iw5_pp90m1_mp_xmags_camo09") return "iw5_pp90m1_mp_xmags_silencer_camo11";
                else if (weapon == "iw5_m9_mp_xmags_camo09") return "iw5_m9_mp_xmags_thermalsmg_camo11";
                else if (weapon == "iw5_mp7_mp_xmags_camo09") return "iw5_mp7_mp_xmags_silencer_camo11";
                else if (weapon == "iw5_dragunov_mp_dragunovscope_xmags_camo09") return "iw5_dragunov_mp_acog_xmags_camo11";
                else if (weapon == "iw5_barrett_mp_barrettscope_xmags_camo09") return "iw5_barrett_mp_acog_xmags_camo11";
                else if (weapon == "iw5_l96a1_mp_l96a1scope_xmags_camo09") return "iw5_l96a1_mp_l96a1scopevz_xmags_camo11";
                else if (weapon == "iw5_rsass_mp_rsassscope_xmags_camo09") return "iw5_rsass_mp_thermal_xmags_camo11";
                else if (weapon == "iw5_sa80_mp_xmags_camo09") return "iw5_sa80_mp_reflexlmg_xmags_camo11";
                else if (weapon == "iw5_mg36_mp_xmags_camo09") return "iw5_mg36_mp_xmags_grip_camo11";
                else if (weapon == "iw5_pecheneg_mp_xmags_camo09") return "iw5_pecheneg_mp_xmags_thermal_camo11";
                else if (weapon == "iw5_mk46_mp_xmags_camo09") return "iw5_mk46_mp_xmags_silencer_camo11";
                else if (weapon == "iw5_usas12_mp_xmags_camo09") return "iw5_usas12_mp_xmags_reflex_camo11";
                else if (weapon == "iw5_ksg_mp_xmags_camo09") return "iw5_ksg_mp_grip_xmags_camo11";
                else if (weapon == "iw5_spas12_mp_xmags_camo09") return "iw5_spas12_mp_grip_xmags_camo11";
                else if (weapon == "iw5_striker_mp_xmags_camo09") return "iw5_striker_mp_xmags_grip_camo11";
            else return "";
        }

        private static void print(string format, params object[] p)
        {
            Log.Write(LogLevel.All, format, p);
        }

        private void loadMapEdit(string mapname)
        {
            try
            {
                StreamReader map = new StreamReader("scripts\\maps\\" + mapname + "_zm.txt");
                while (!map.EndOfStream)
                {
                    string line = map.ReadLine();
                    if (line.StartsWith("//") || line.Equals(string.Empty))
                    {
                        continue;
                    }
                    string[] split = line.Split(':');
                    if (split.Length < 1)
                    {
                        continue;
                    }
                    string type = split[0];
                    switch (type)
                    {
                        case "crate":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            spawnCrate(parseVec3(split[0]), parseVec3(split[1]), false, false);
                            break;
                        case "ramp":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            CreateRamp(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "elevator":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            CreateElevator(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "door":
                            split = split[1].Split(';');
                            if (split.Length < 7) continue;
                            CreateDoor(parseVec3(split[0]), parseVec3(split[1]), parseVec3(split[2]), int.Parse(split[3]), int.Parse(split[4]), int.Parse(split[5]), int.Parse(split[6]));
                            break;
                        case "wall":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            CreateWall(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "invisiblewall":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            CreateInvisibleWall(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "deathwall":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            CreateDeathWall(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "randombox":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            randomWeaponCrate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "pap":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            papCrate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "upgradebox":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            upgradeCrate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "gambler":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            gamblerCrate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "floor":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            CreateFloor(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        /*case "realelevator":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            realElevator(parseVec3(split[0]), parseVec3(split[1]));
                            break;*/
                        case "model":
                            split = split[1].Split(';');
                            if (split.Length < 3) continue;
                            spawnModel(split[0], parseVec3(split[1]), parseVec3(split[2]));
                            break;
                        case "perk1":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk1Crate(parseVec3(split[0]), parseVec3(split[1]), false);
                            break;
                        case "perk1Interchange":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk1Crate(parseVec3(split[0]), parseVec3(split[1]), true);
                            break;
                        case "perk2":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk2Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "perk3":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk3Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "perk4":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk4Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "perk5":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk5Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "perk6":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk6Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "perk7":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk7Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "perk8":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Perk8Crate(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "claymore":
                            split = split[1].Split(';');
                            if (split.Length < 2) continue;
                            Claymore(parseVec3(split[0]), parseVec3(split[1]));
                            break;
                        case "wallweapon":
                            split = split[1].Split(';');
                            if (split.Length < 4) continue;
                            WallWeapon(parseVec3(split[0]), parseVec3(split[1]), split[2], int.Parse(split[3]));
                            break;
                        default:
                            print("Unknown MapEdit Entry {0}... ignoring", type);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                print("error loading mapedit for map {0}: {1}", mapname, e.Message);
            }
        }

        private Vector3 parseVec3(string vec3)
        {
            vec3 = vec3.Replace(" ", string.Empty);
            if (!vec3.StartsWith("(") && !vec3.EndsWith(")")) throw new IOException("Malformed MapEdit File!");
            vec3 = vec3.Replace("(", string.Empty);
            vec3 = vec3.Replace(")", string.Empty);
            String[] split = vec3.Split(',');
            if (split.Length < 3) throw new IOException("Malformed MapEdit File!");
            return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
        }

        private string getAlliesFlagModel(string mapname)
        {
            switch (mapname)
            {
                case "mp_alpha":
                case "mp_dome":
                case "mp_exchange":
                case "mp_hardhat":
                case "mp_interchange":
                case "mp_lambeth":
                case "mp_radar":
                case "mp_cement":
                case "mp_hillside_ss":
                case "mp_morningwood":
                case "mp_overwatch":
                case "mp_park":
                case "mp_qadeem":
                case "mp_restrepo_ss":
                case "mp_terminal_cls":
                case "mp_roughneck":
                case "mp_boardwalk":
                case "mp_moab":
                case "mp_nola":
                    return "prop_flag_delta";
                case "mp_bootleg":
                case "mp_bravo":
                case "mp_carbon":
                case "mp_mogadishu":
                case "mp_village":
                case "mp_shipbreaker":
                    return "prop_flag_pmc";
                case "mp_paris":
                    return "prop_flag_gign";
                case "mp_plaza2":
                case "mp_seatown":
                case "mp_underground":
                case "mp_aground_ss":
                case "mp_courtyard_ss":
                case "mp_italy":
                case "mp_meteora":
                    return "prop_flag_sas";
            }
            return "";
        }
        private string getAxisFlagModel(string mapname)
        {
            switch (mapname)
            {
                case "mp_alpha":
                case "mp_bootleg":
                case "mp_dome":
                case "mp_exchange":
                case "mp_hardhat":
                case "mp_interchange":
                case "mp_lambeth":
                case "mp_paris":
                case "mp_plaza2":
                case "mp_radar":
                case "mp_underground":
                case "mp_cement":
                case "mp_hillside_ss":
                case "mp_overwatch":
                case "mp_park":
                case "mp_restrepo_ss":
                case "mp_terminal_cls":
                case "mp_roughneck":
                case "mp_boardwalk":
                case "mp_moab":
                case "mp_nola":
                    return "prop_flag_speznas";
                case "mp_bravo":
                case "mp_carbon":
                case "mp_mogadishu":
                case "mp_village":
                case "mp_shipbreaker":
                    return "prop_flag_africa";
                case "mp_seatown":
                case "mp_aground_ss":
                case "mp_courtyard_ss":
                case "mp_meteora":
                case "mp_morningwood":
                case "mp_qadeem":
                case "mp_italy":
                    return "prop_flag_ic";
            }
            return "";
        }
        private void GetRandomSpawnForMap(Entity player)
        {
            int Spawn = Call<int>("randomint", 4);
                string Mapname = Call<string>("getdvar", "mapname");
            Vector3[] Spawns = new Vector3[4];
            switch (Mapname)
            {
                case "mp_dome":
                    //Vector3[] Spawns = new Vector3[4];
                    Spawns = new []{ new Vector3(1738.37f, 1370.556f, -254.875f), new Vector3(1348.059f, -431.1246f, -380.0385f), new Vector3(-1536.661f, 1364.875f, -427.875f), new Vector3(1206.948f, 2449.295f, -254.875f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_alpha":
                    Spawns = new [] { new Vector3(-2102.126f, 2386.535f, 130.125f), new Vector3(274.4388f, 2194.083f, 0.1249864f), new Vector3(-1796.311f, -430.2962f, 0.1249976f), new Vector3(-2330.441f, 1237.204f, 0.1249976f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_bootleg":
                    Spawns = new [] { new Vector3(466.9774f, -2113.352f, 10.23987f), new Vector3(-1492.965f, -1556.094f, 2.125002f), new Vector3(-1477.892f, 1672.92f, -91.63445f), new Vector3(-2012.766f, 446.4677f, -48.32563f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_bravo":
                    Spawns = new [] { new Vector3(-329.757f, -1040.054f, 972.125f), new Vector3(-1933.714f, -86.98907f, 941.9239f), new Vector3(-2081.902f, 1083.905f, 1090.354f), new Vector3(-289.4247f, 1575.982f, 1199.904f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_carbon":
                    Spawns = new [] { new Vector3(720.3721f, -3402.198f, 3949.125f), new Vector3(-3175.421f, -4869.863f, 3584.29f), new Vector3(-3844.975f, -4400.346f, 3593.733f), new Vector3(-3352.803f, -2576.618f, 3743.674f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_exchange":
                    Spawns = new [] { new Vector3(-1215.336f, 1292.031f, 63.47081f), new Vector3(-648.0423f, -392.9717f, 69.125f), new Vector3(-188.9146f, -387.1355f, 69.125f), new Vector3(-248.0848f, -1916.667f, 36.125f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_hardhat":
                    Spawns = new [] { new Vector3(-406.5136f, -1618.406f, 288.125f), new Vector3(-1078.53f, 382.9696f, 197.7459f), new Vector3(-184.6946f, 1250.022f, 376.125f), new Vector3(1783.467f, 1424.673f, 317.112f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_interchange":
                    Spawns = new [] { new Vector3(1438.157f, -3299.12f, 126.3327f), new Vector3(-931.3969f, -1456.446f, 125.829f), new Vector3(-1663.76f, -427.3478f, 51.79729f), new Vector3(-1276.923f, 219.6574f, 19.45426f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_lambeth":
                    Spawns = new [] { new Vector3(395.6884f, 1627.158f, -227.8324f), new Vector3(-1531.521f, 748.4872f, -248.875f), new Vector3(-82.49342f, -1666.9f, -238.5572f), new Vector3(2976.283f, -863.1669f, -262.8041f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_mogadishu":
                    Spawns = new [] { new Vector3(-1014.937f, 3044.837f, 122.561f), new Vector3(1681.864f, 2660.679f, 49.71062f), new Vector3(1820.755f, 189.6846f, -91.71303f), new Vector3(1304.945f, -1451.21f, -45.51621f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_paris":
                    Spawns = new [] { new Vector3(377.4136f, -1333.604f, 6.590031f), new Vector3(1626.601f, -522.9637f, -13.43833f), new Vector3(2139.003f, 636.3165f, -15.875f), new Vector3(-1524.35f, -159.5904f, 186.125f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_plaza2":
                    Spawns = new [] { new Vector3(462.6574f, 1954.559f, 608.125f), new Vector3(839.8373f, 133.5983f, 648.125f), new Vector3(946.7666f, -928.2232f, 675.125f), new Vector3(-24.42895f, -1972.24f, 608.125f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_radar":
                    Spawns = new [] { new Vector3(-7579.615f, 3730.548f, 1360.125f), new Vector3(-7355.622f, 3406.38f, 1360.125f), new Vector3(-7428.89f, 4807.238f, 1352.625f), new Vector3(-4449.667f, 4516.907f, 1208.125f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_seatown":
                    Spawns = new [] { new Vector3(170.6411f, -1477.855f, 208.125f), new Vector3(1277.743f, -1325.823f, 208.125f), new Vector3(-1009.149f, 1512.719f, 237.374f), new Vector3(-2382.277f, -1826.592f, 288.125f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_underground":
                    Spawns = new [] { new Vector3(-1359.654f, 201.5539f, -55.875f), new Vector3(1096.871f, 2250.78f, -119.875f), new Vector3(639.1808f, 479.8946f, -191.875f), new Vector3(-143.5202f, -140.8756f, -127.875f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_village":
                    Spawns = new [] { new Vector3(241.7594f, -3318.613f, 369.2305f), new Vector3(-1897.025f, -1387.325f, 367.5302f), new Vector3(-1093.253f, 1641.847f, 130.9021f), new Vector3(1486.07f, 1349.393f, 237.1845f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                case "mp_terminal_cls":
                    Spawns = new [] { new Vector3(527.1624f, 4896.356f, 41.4566f), new Vector3(2651.442f, 2762.054f, 40.125f), new Vector3(2884.739f, 4474.594f, 192.125f), new Vector3(1072.455f, 7437.27f, 192.125f) };
                    player.Call("setorigin", Spawns[Spawn]);
                    break;
                default:
                    Call("iprintlnbold", "^1Zombie spawns not supported in this level. Contact ^2Slvr99^1!");
                    break;
            }
        }

        private void SpawnInZombiePrison(Entity player)
        {
            string Mapname = Call<string>("getdvar", "mapname");
            switch (Mapname)
            {
            case "mp_dome":
                player.Call("setorigin", new Parameter(new Vector3(1224.118f, 2638.699f, -254.875f)));
                    break;
                case "mp_alpha":
                player.Call("setorigin", new Parameter(new Vector3(-1052.848f, 3024.331f, 292.125f)));
                    break;
                case "mp_bootleg":
                player.Call("setorigin", new Parameter(new Vector3(2881.628f, -611.3768f, 340.125f)));
                    break;
                case "mp_bravo":
                player.Call("setorigin", new Parameter(new Vector3(-2479.283f, -271.3307f, 939.7438f)));
                    break;
                case "mp_carbon":
                player.Call("setorigin", new Parameter(new Vector3(-1308.62f, -7619.679f, 4408.125f)));
                    break;
                case "mp_exchange":
                player.Call("setorigin", new Parameter(new Vector3(1250.283f, 2350.778f, 687.125f)));
                    break;
                case "mp_hardhat":
                player.Call("setorigin", new Parameter(new Vector3(2336.611f, 767.494f, 456.125f)));
                    break;
                case "mp_interchange":
                player.Call("setorigin", new Parameter(new Vector3(3822.359f, -84.25299f, 124.625f)));
                    break;
                case "mp_lambeth":
                player.Call("setorigin", new Parameter(new Vector3(2183.529f, -910.7379f, -255.875f)));
                    break;
                case "mp_mogadishu":
                player.Call("setorigin", new Parameter(new Vector3(-1655.729f, -683.8426f, 2.125f)));
                    break;
                case "mp_paris":
                player.Call("setorigin", new Parameter(new Vector3(919.4839f, -2091.714f, 26.22339f)));
                    break;
                case "mp_plaza2":
                player.Call("setorigin", new Parameter(new Vector3(-2742.552f, 1686.967f, 968.125f)));
                    break;
                case "mp_radar":
                player.Call("setorigin", new Parameter(new Vector3(-2230.524f, 4423.24f, 1166.725f)));
                    break;
                case "mp_seatown":
                player.Call("setorigin", new Parameter(new Vector3(-2166.174f, -3263.711f, 448.125f)));
                    break;
                case "mp_underground":
                player.Call("setorigin", new Parameter(new Vector3(1287.944f, 102.1815f, 8.124999f)));
                    break;
                case "mp_village":
                player.Call("setorigin", new Parameter(new Vector3(1169.042f, -3831.404f, 371.5968f)));
                    break;
                case "mp_terminal_cls":
                player.Call("setorigin", new Parameter(new Vector3(-2149.853f, 4270.342f, 192.8578f)));
                    break;
                default:
                    Call("iprintlnbold", "Map not supported. Contact ^2Slvr99");
                    break;
            }
        }
    }
}