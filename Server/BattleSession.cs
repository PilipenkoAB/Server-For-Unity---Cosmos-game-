using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Server
{
    /*
         Battle1v1AI - class for battle, contain all information about the battle
           and all actions that can be in that battle--------------- 
       */
    public class BattleSession
    {

        // Constructor that takes - starting point no arguments:
        public BattleSession(int[] playersToSetStart, int[] playersTeamToSetStart, string[] mapIdStart)
        {

            mapId = Convert.ToInt32(mapIdStart[0]);

            // TEST SYSTEM
            string[] positionsTeam1All = mapIdStart[1].Split('|');

            string[] positionsTeam1X = new string[positionsTeam1All.Length];
            string[] positionsTeam1Y = new string[positionsTeam1All.Length];
            string[] positionsTeam1Rotation = new string[positionsTeam1All.Length];

            for (int i = 0; i < positionsTeam1All.Length; i++)
            {
                string[] positionsTeam1Separated = positionsTeam1All[i].Split(';');

                positionsTeam1X[i] = positionsTeam1Separated[0];
                positionsTeam1Y[i] = positionsTeam1Separated[1];
                positionsTeam1Rotation[i] = positionsTeam1Separated[2];
            }



            string[] positionsTeam2All = mapIdStart[2].Split('|');

            string[] positionsTeam2X = new string[positionsTeam2All.Length];
            string[] positionsTeam2Y = new string[positionsTeam2All.Length];
            string[] positionsTeam2Rotation = new string[positionsTeam2All.Length];

            for (int i = 0; i < positionsTeam2All.Length; i++)
            {
                string[] positionsTeam2Separated = positionsTeam2All[i].Split(';');

                positionsTeam2X[i] = positionsTeam2Separated[0];
                positionsTeam2Y[i] = positionsTeam2Separated[1];
                positionsTeam2Rotation[i] = positionsTeam2Separated[2];
            }



            //----
            playersToSet = playersToSetStart;
            playersTeamsToSet = playersTeamToSetStart;

            // create empty slots for players
            int positionTeam1i = 0;
            int positionTeam2i = 0;
            for (int i = 0; i < playersToSet.Length; i++)
            {
                players.Add(new PlayerBattleInformation());
                players[i].playerType = playersToSet[i];
                players[i].playerTeam = playersTeamsToSet[i];

                //set position
                if (players[i].playerTeam == 0)
                {
                    players[i].playerPositionX = Convert.ToDouble(positionsTeam1X[positionTeam1i]);
                    players[i].playerPositionY = Convert.ToDouble(positionsTeam1Y[positionTeam1i]);
                    players[i].playerPositionRotation = Convert.ToDouble(positionsTeam1Rotation[positionTeam1i]);
                    positionTeam1i += 1;
                }
                else if (players[i].playerTeam == 1)
                {
                    players[i].playerPositionX = Convert.ToDouble(positionsTeam2X[positionTeam2i]);
                    players[i].playerPositionY = Convert.ToDouble(positionsTeam2Y[positionTeam2i]);
                    players[i].playerPositionRotation = Convert.ToDouble(positionsTeam2Rotation[positionTeam2i]);
                    positionTeam2i += 1;
                }
                else
                {
                    players[i].playerPositionX = 0;
                    players[i].playerPositionY = 0;
                    players[i].playerPositionRotation = 0;
                }

            }

            toStart = 0;
            started = 0;
            finished = 0;
            battleTime = 0;
    }


      // look through available slots and add requested player or AI to the slot
        public int AddPlayer(int playerType, int playerTeam, int playerId) // new
        {
            int idOfArray = -1;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerType == playerType && players[i].playerTeam == playerTeam && players[i].playerId == 0) 
                {
                    players[i].playerId = playerId;
                    idOfArray = i;
                    return idOfArray;
                }
            }
            return idOfArray;
        }

        public int RequestForIdInArray(int playerId)  // work only for player, not AI
        {
            int idOfArray = -1;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerId == playerId && players[i].playerType == 0)
                {
                    idOfArray = i;
                }
            }
            return idOfArray;
        }




        // give information at start to player who asked that information 
        public string RequestForStartPlayerInformation(int idInArray) 
        {
            string answer = "";
            //=======================================
            //          System information
            //=======================================
            answer = answer + "";
            answer = answer + "|";
            //=======================================
            //          Environment information
            //=======================================
            answer = answer + "";
            answer = answer + "|";

            //=======================================
            //          This Player information
            //=======================================

            answer = answer + players[idInArray].playerTeam;
            answer = answer + ";";

            answer = answer + players[idInArray].playerShipId;
            answer = answer + ";";

            answer = answer + String.Format("{0:0.0}", players[idInArray].playerPositionX);
            answer = answer + ",";
            answer = answer + String.Format("{0:0.0}", players[idInArray].playerPositionY);
            answer = answer + ",";
            answer = answer + String.Format("{0:0.0}", players[idInArray].playerPositionRotation);
            answer = answer + ";";

            answer = answer + String.Format("{0:0.0}", players[idInArray].playerVisionRadius);
            answer = answer + ";";

            answer = answer + players[idInArray].playerShipMaxHealth;
            answer = answer + ";";
            answer = answer + players[idInArray].playerShipMaxEnergy;
            answer = answer + ";";

            for (int i = 0; i < 17; i++)
            {
                answer = answer + players[idInArray].playerSlotExist[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotHealth[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotPowered[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotEnergyRequired[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotType[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotAdditionalInfoToClient[i];
                answer = answer + ",";
            }

            answer = answer.Remove(answer.Length - 1, 1); // remove last ";"
            answer = answer + ";";

            for (int i = 0; i < 5; i++)
            {
                answer = answer + players[idInArray].playerWeaponSlotExist[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotPowered[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotEnergyRequired[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotDamage[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotReloadTime[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotCurrentReloadTime[i];
                answer = answer + ",";
            }

            answer = answer.Remove(answer.Length - 1, 1); // remove last ";"


            //=================================
            //          other players
            //=================================
            answer = answer + "|";

            for (int i = 0; i < players.Count; i++)
            {
                if (players[idInArray] != players[i]) // don't choose the player who ask the information
                {
                    if (players[idInArray].playerTeam == players[i].playerTeam) // if players from the same team
                    {
                        answer = answer + Convert.ToString(i); // IdInTheArray
                        answer = answer + ";";
                        answer = answer + players[i].playerTeam;
                        answer = answer + ";";
                        answer = answer + players[i].playerShipId;
                        answer = answer + ";";

                        answer = answer + String.Format("{0:0.0}", players[i].playerPositionX);
                        answer = answer + ",";
                        answer = answer + String.Format("{0:0.0}", players[i].playerPositionY);
                        answer = answer + ",";
                        answer = answer + String.Format("{0:0.0}", players[i].playerPositionRotation);
                        answer = answer + ";";

                        answer = answer + players[i].playerShipMaxHealth;
                        answer = answer + "|";
                    }
                    else if (players[idInArray].playerTeam != players[i].playerTeam) // if players from the other team
                    {
                        //check vision
                        double distaneBetweenPlayers = Math.Sqrt(Math.Pow(players[idInArray].playerPositionX - players[i].playerPositionX,2)+ Math.Pow(players[idInArray].playerPositionY - players[i].playerPositionY, 2));

                        if (distaneBetweenPlayers <= players[idInArray].playerVisionRadius) 
                        {
                            answer = answer + Convert.ToString(i); // IdInTheArray
                            answer = answer + ";";
                            answer = answer + players[i].playerTeam;
                            answer = answer + ";";
                            answer = answer + players[i].playerShipId;
                            answer = answer + ";";

                            answer = answer + String.Format("{0:0.0}", players[i].playerPositionX);
                            answer = answer + ",";
                            answer = answer + String.Format("{0:0.0}", players[i].playerPositionY);
                            answer = answer + ",";
                            answer = answer + String.Format("{0:0.0}", players[i].playerPositionRotation);
                            answer = answer + ";";

                            answer = answer + players[i].playerShipMaxHealth;
                            answer = answer + "|";
                        }
                    }
                }
            }

            answer = answer.Remove(answer.Length - 1, 1); // remove last ";"

            //============================================

            return answer; 
        }




        // give information at update to player who asked that information 
        public string RequestForUpdatePlayerInformation(int idInArray) 
        {
            string answer = "";
            //=======================================
            //          System information
            //=======================================
            answer = answer + battleTime;
            answer = answer + "|";
            //=======================================
            //          Environment information
            //=======================================
            answer = answer + "";
            answer = answer + "|";

            //=======================================
            //          This Player information
            //=======================================

            answer = answer + String.Format("{0:0.0}", players[idInArray].playerPositionX); 
            answer = answer + ",";
            answer = answer + String.Format("{0:0.0}", players[idInArray].playerPositionY);
            answer = answer + ",";
            answer = answer + String.Format("{0:0.0}", players[idInArray].playerPositionRotation);
            answer = answer + ";";

            answer = answer + players[idInArray].playerFocus;
            answer = answer + ";";

            answer = answer + players[idInArray].playerShipCurrentHealth;
            answer = answer + ";";
            answer = answer + players[idInArray].playerShipFreeEnergy;
            answer = answer + ";";

            for (int i = 0; i < 17; i++)
            {
                answer = answer + players[idInArray].playerSlotExist[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotHealth[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotPowered[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotEnergyRequired[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotType[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerSlotAdditionalInfoToClient[i];
                answer = answer + ",";
            }

            answer = answer.Remove(answer.Length - 1, 1); // remove last ";"
            answer = answer + ";";

            for (int i = 0; i < 5; i++)
            {
                answer = answer + players[idInArray].playerWeaponSlotExist[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotPowered[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotEnergyRequired[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotDamage[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotReloadTime[i];
                answer = answer + ",";
                answer = answer + players[idInArray].playerWeaponSlotCurrentReloadTime[i];
                answer = answer + ",";
            }

            answer = answer.Remove(answer.Length - 1, 1); // remove last ";"


            //=================================
            //          other players
            //=================================
            answer = answer + "|";

            for (int i = 0; i < players.Count; i++)
            {
                if (players[idInArray] != players[i]) // don't choose the player who ask the information
                {
                    if (players[idInArray].playerTeam == players[i].playerTeam) // if players from the same team
                    {
                        answer = answer + Convert.ToString(i); // IdInTheArray
                        answer = answer + ";";
                        answer = answer + players[i].playerTeam;
                        answer = answer + ";";
                        answer = answer + players[i].playerShipId;
                        answer = answer + ";";

                        answer = answer + String.Format("{0:0.0}", players[i].playerPositionX);
                        answer = answer + ",";
                        answer = answer + String.Format("{0:0.0}", players[i].playerPositionY);
                        answer = answer + ",";
                        answer = answer + String.Format("{0:0.0}", players[i].playerPositionRotation);
                        answer = answer + ";";

                        answer = answer + players[i].playerShipMaxHealth;
                        answer = answer + "|";
                    }
                    else if (players[idInArray].playerTeam != players[i].playerTeam) // if players from the other team
                    {
                        //check vision
                        double distaneBetweenPlayers = Math.Sqrt(Math.Pow(players[idInArray].playerPositionX - players[i].playerPositionX, 2) + Math.Pow(players[idInArray].playerPositionY - players[i].playerPositionY, 2));

                        if (distaneBetweenPlayers <= players[idInArray].playerVisionRadius)
                        {
                            answer = answer + Convert.ToString(i); // IdInTheArray
                            answer = answer + ";";
                            answer = answer + players[i].playerTeam;
                            answer = answer + ";";
                            answer = answer + players[i].playerShipId;
                            answer = answer + ";";

                            answer = answer + String.Format("{0:0.0}", players[i].playerPositionX);
                            answer = answer + ",";
                            answer = answer + String.Format("{0:0.0}", players[i].playerPositionY);
                            answer = answer + ",";
                            answer = answer + String.Format("{0:0.0}", players[i].playerPositionRotation);
                            answer = answer + ";";

                            answer = answer + players[i].playerShipMaxHealth;
                            answer = answer + "|";
                        }
                    }
                }
            }

            answer = answer.Remove(answer.Length - 1, 1); // remove last ";"

            //============================================
            Console.WriteLine("packet size = "+ answer.Length);
            return answer;
        }
        //--------------



        public void SetStartHealth()
        {
            for (int i = 0; i < players.Count; i++)
            {
                players[i].playerShipCurrentHealth = players[i].playerShipMaxHealth;

            }
        }

        public void SetStartReload()
        {
            for (int i = 0; i < players.Count; i++)
            {
                for (int ii = 0; ii < 5; ii++)
                {
                    if (players[i].playerWeaponSlotExist[ii] == 1)
                    {
                        players[i].playerWeaponSlotCurrentReloadTime[ii] = players[i].playerWeaponSlotReloadTime[ii];
                    }
                }
            }
        }


        public void ReloadAllWeaponsPerTick()
        {
            int reloadOneTick = 50; // ms

            for (int i = 0; i < players.Count; i++)
            {
                // reload of the player weapon
                for (int ii = 0; ii < players[i].playerWeaponSlotCurrentReloadTime.Length; ii++)
                {
                    if (players[i].playerWeaponSlotCurrentReloadTime[ii] > 0 && players[i].playerWeaponSlotPowered[ii] > 0)
                    {
                        players[i].playerWeaponSlotCurrentReloadTime[ii] -= reloadOneTick;

                    }
                    else if (players[i].playerWeaponSlotCurrentReloadTime[ii] <= 0)
                    {
                        players[i].playerWeaponSlotCurrentReloadTime[ii] = 0;
                    }
                }
            }
        }


        public void ReloadAllShieldsPerTick() 
        {
            int reloadOneTick = 50; // ms

            int playerSumCapasity = 0;

            for (int i = 0; i < players.Count; i++)
            {
                //player shields
                for (int ii = 0; ii < 5; ii++)
                {
                    if (players[i].playerSlotExist[ii + 2] != 0 && players[i].playerSlotExist[ii + 2] != -1 && players[i].playerSlotType[ii + 2] == "shield" && players[i].playerSlotPowered[ii + 2] <= 0)
                    {
                        players[i].playerSlotShieldCurrentCapacity[ii + 2] = 0;
                        players[i].playerSlotShieldRechargeCurrentTime[ii + 2] = players[i].playerSlotShieldRechargeTime[ii + 2];
                    }
                    else if (players[i].playerSlotExist[ii + 2] != 0 && players[i].playerSlotExist[ii + 2] != -1 && players[i].playerSlotType[ii + 2] == "shield" && players[i].playerSlotPowered[ii + 2] > 0 && players[i].playerSlotHealth[ii + 2] > 0 && players[i].playerSlotShieldRechargeCurrentTime[ii + 2] > 0)
                    {
                        players[i].playerSlotShieldRechargeCurrentTime[ii + 2] -= reloadOneTick;
                    }
                    else if (players[i].playerSlotShieldRechargeCurrentTime[ii + 2] <= 0)
                    {
                        players[i].playerSlotShieldCurrentCapacity[ii + 2] += players[i].playerSlotShieldRechargeRate[ii + 2];

                        if (players[i].playerSlotShieldCurrentCapacity[ii + 2] > players[i].playerSlotShieldCapacity[ii + 2])
                        {
                            players[i].playerSlotShieldCurrentCapacity[ii + 2] = players[i].playerSlotShieldCapacity[ii + 2];
                        }
                        players[i].playerSlotShieldRechargeCurrentTime[ii + 2] = players[i].playerSlotShieldRechargeTime[ii + 2];
                    }
                    playerSumCapasity += players[i].playerSlotShieldCurrentCapacity[ii + 2];
                    // sum capacity
                    players[i].playerSumShieldCurrentCapacity = playerSumCapasity;
                }
            }
        }


        public void UpdateAllFocus() 
        {
            for (int i = 0; i < players.Count; i++)
            {
                // if focus not in vision - > remove focus
                if (players[i].playerFocus != 0) 
                {
                    double distanceToPoint = Math.Sqrt(Math.Pow(players[players[i].playerFocus].playerPositionX - players[i].playerPositionX, 2) + Math.Pow(players[players[i].playerFocus].playerPositionY - players[i].playerPositionY, 2));

                    if (distanceToPoint > players[i].playerVisionRadius)
                    {
                        players[i].playerFocus = 0;
                    }
                }
            }
        }
        //-----------------------

        // not sure about that one 
        public void AIAttackAllWeaponsCooldown(int aIId) 
        {
            for (int i = 0; i < players[aIId].playerWeaponSlotCurrentReloadTime.Length; i++)
            {
                if (players[aIId].playerWeaponSlotCurrentReloadTime[i] == 0 && players[aIId].playerSlotType[i] == "weaponcontrol" && players[aIId].playerSlotPowered[i] == 1)
                {
                    players[aIId].playerWeaponSlotProjectileTime1[i, 0] = 1500; // need to set if how many projectles ?????
                    players[aIId].playerWeaponSlotCurrentReloadTime[i] = players[aIId].playerWeaponSlotReloadTime[i];

                    //get random slot to attack
                    Random randomSlotToAttack = new Random();
                    int slotToAttack = randomSlotToAttack.Next(0,11); // 11 because not small slots to attack
                    while (players[aIId].playerSlotExist[slotToAttack] < 0) 
                    {
                        slotToAttack = randomSlotToAttack.Next(0, 11);
                    }
                    players[aIId].playerWeaponSlotProjectileAimModule[i] = slotToAttack;
                }
            }
        }


        public void AIPowerModules(int aIId) 
        {
            for (int i = 0; i < players[aIId].playerSlotExist.Length; i++)
            {
                if (players[aIId].playerSlotExist[i] > 0 && players[aIId].playerSlotPowered[i] != 1 && players[aIId].playerSlotHealth[i] > 0 && players[aIId].playerShipFreeEnergy > 0)
                {
                    players[aIId].playerSlotPowered[i] = 1;
                    players[aIId].playerShipFreeEnergy -= 1;
                }
            }

            for (int i = 0; i < players[aIId].playerWeaponSlotExist.Length; i++)
            {
                if (players[aIId].playerWeaponSlotExist[i] > 0 && players[aIId].playerWeaponSlotPowered[i] != 1 && players[aIId].playerShipFreeEnergy > 0)
                {
                    players[aIId].playerWeaponSlotPowered[i] = 1;
                    players[aIId].playerShipFreeEnergy -= 1;
                }
            }
        }
        //----------------





        // that is broken for sure
        public bool PlayerAttackModule(int weaponIdint, int moduleSlotId, int playerId) 
        {
            // player attack AI
            //if weapon control exist and powered
            
            for (int i = 0; i < players[playerId].playerSlotType.Length; i++)
            {
                if (players[playerId].playerWeaponSlotCurrentReloadTime[weaponIdint] == 0 && players[playerId].playerSlotType[i] == "weaponcontrol" && players[playerId].playerSlotPowered[i] == 1 && players[playerId].playerWeaponSlotPowered[weaponIdint] == 1)
                {
                    players[playerId].playerWeaponSlotProjectileTime[weaponIdint] = 1500;
                    players[playerId].playerWeaponSlotCurrentReloadTime[weaponIdint] = players[playerId].playerWeaponSlotReloadTime[weaponIdint];
                    players[playerId].playerWeaponSlotProjectileAimModule[weaponIdint] = moduleSlotId;
                    // shot is happened
                    return true;
                }

            }

            return false;
        }

        // that is broken for sure
        public void ProjectilesMoveTime() 
        {
            int reloadOneTick = 50; // ms

            for (int i = 0; i < players[0].playerWeaponSlotProjectileTime.Length; i++)
            {
                if (players[0].playerWeaponSlotProjectileTime[i] > 0)
                {

                    players[0].playerWeaponSlotProjectileTime[i] -= reloadOneTick;

                    if (players[0].playerWeaponSlotProjectileTime[i] <= 0) 
                    {
                        // projectile hit the enemyAI
                        Random randomChanceToHit = new Random();
                        int ChanceToHit = randomChanceToHit.Next(0, 100);

                        Random randomDamage = new Random();
                        int damageToShip = randomDamage.Next(Convert.ToInt32(Convert.ToDouble(players[0].playerWeaponSlotDamage[i]) * 0.9), Convert.ToInt32(Convert.ToDouble(players[0].playerWeaponSlotDamage[i]) * 1.1));
                        int resultShieldCapasityAfterDamage = 0;

                        if (ChanceToHit <= 50) // if hit the ship but not module
                        {
                            for (int i1 = 0; i1 < 5; i1++)
                            {
                                // layers of the shields. first - destroy first layer, then next then next
                                if (players[1].playerSlotType[i1 + 2] == "shield" && players[1].playerSlotHealth[i1 + 2] > 0 && players[1].playerSlotPowered[i1 + 2] > 0)
                                {
                                    // in progress !!!
                                    resultShieldCapasityAfterDamage = players[1].playerSlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                    if (resultShieldCapasityAfterDamage <= 0)
                                    {
                                        players[1].playerSlotShieldCurrentCapacity[i1 + 2] = 0;
                                        damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                    }
                                    else
                                    {
                                        players[1].playerSlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                        damageToShip = 0;
                                    }
                                }
                            }

                            // damage to ship if no shields (all damage in 1 take)
                            players[1].playerShipCurrentHealth -= damageToShip;
                            damageToShip = 0;
                        } 
                        else if(ChanceToHit > 50 && ChanceToHit <= 75) // if hit the module (half damage to ship, half damage to module
                        {
                            for (int i1 = 0; i1 < 5; i1++)
                            {
                                // layers of the shields. first - destroy first layer, then next then next
                                if (players[1].playerSlotType[i1 + 2] == "shield" && players[1].playerSlotHealth[i1 + 2] > 0 && players[1].playerSlotPowered[i1 + 2] > 0)
                                {
                                    // in progress !!!
                                    resultShieldCapasityAfterDamage = players[1].playerSlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                    if (resultShieldCapasityAfterDamage <= 0)
                                    {
                                        players[1].playerSlotShieldCurrentCapacity[i1 + 2] = 0;
                                        damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                    }
                                    else
                                    {
                                        players[1].playerSlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                        damageToShip = 0;
                                    }
                                }
                            }

                            players[1].playerShipCurrentHealth -= damageToShip / 2;
                            players[1].playerSlotHealth[players[0].playerWeaponSlotProjectileAimModule[i]] -= damageToShip / 2;

                            if (players[1].playerSlotHealth[players[0].playerWeaponSlotProjectileAimModule[i]] <= 0)
                            {
                                players[1].playerShipCurrentHealth -= damageToShip / 2; // aditional damage if module was destroyed

                                players[1].playerSlotHealth[players[0].playerWeaponSlotProjectileAimModule[i]] = 0;
                                players[1].playerSlotPowered[players[0].playerWeaponSlotProjectileAimModule[i]] = 0;

                                if (players[1].playerSlotPowered[players[0].playerWeaponSlotProjectileAimModule[i]] > 0)
                                {
                                    players[1].playerSlotPowered[players[0].playerWeaponSlotProjectileAimModule[i]] = 0;
                                    players[1].playerShipFreeEnergy += 1;

                                    if (players[1].playerSlotType[players[0].playerWeaponSlotProjectileAimModule[i]] == "weaponcontrol")
                                    {
                                        for (int iii = 0; iii < players[1].playerWeaponSlotPowered.Length; iii++)
                                        {
                                            if (players[1].playerWeaponSlotPowered[iii] > 0)
                                            {
                                                players[1].playerShipFreeEnergy += 1;
                                                players[1].playerWeaponSlotPowered[iii] = 0;
                                            }
                                        }
                                    }
                                }
                            }

                            damageToShip = 0;
                        }
                        // if missed
                        //-- nothing(?)


                        players[0].playerWeaponSlotProjectileTime[i] = -1; 
                    }
                }
            }


            //ai (multiple projectiles + shields)
            for (int i = 0; i < 5; i++) //weapon N
            {
                for (int ii = 0; ii < 5; ii++) //projectile N
                {
                    if (players[1].playerWeaponSlotProjectileTime1[i,ii] > 0)
                    {
                        players[1].playerWeaponSlotProjectileTime1[i,ii] -= reloadOneTick;

                        if (players[1].playerWeaponSlotProjectileTime1[i,ii] <= 0)
                        {
                            // projectile hit the enemyAI
                            Random randomChanceToHit = new Random();
                            int ChanceToHit = randomChanceToHit.Next(0, 100);

                            Random randomDamage = new Random();
                            int damageToShip = randomDamage.Next(Convert.ToInt32(Convert.ToDouble(players[1].playerWeaponSlotDamage[i]) * 0.9), Convert.ToInt32(Convert.ToDouble(players[1].playerWeaponSlotDamage[i]) * 1.1));
                            int resultShieldCapasityAfterDamage = 0;

                            if (ChanceToHit <= 50) // if hit the ship but not module
                            {
                                // damage with shields

                                for (int i1 = 0; i1 < 5; i1++)
                                {
                                    // layers of the shields. first - destroy first layer, then next then next
                                    if (players[0].playerSlotType[i1 + 2] == "shield" && players[0].playerSlotHealth[i1 + 2] > 0 && players[0].playerSlotPowered[i1 + 2] > 0)
                                    {
                                        // in progress !!!
                                        resultShieldCapasityAfterDamage = players[0].playerSlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                        if (resultShieldCapasityAfterDamage <= 0)
                                        {
                                            players[0].playerSlotShieldCurrentCapacity[i1 + 2] = 0;
                                            damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                        }
                                        else
                                        {
                                            players[0].playerSlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                            damageToShip = 0;
                                        }
                                    }
                                }

                                // damage to ship if no shields (all damage in 1 take)
                                players[0].playerShipCurrentHealth -= damageToShip;
                                damageToShip = 0;
                            }
                            else if (ChanceToHit > 50 && ChanceToHit <= 75) // if hit the module (half damage to ship, half damage to module
                            {
                                for (int i1 = 0; i1 < 5; i1++)
                                {
                                    // layers of the shields. first - destroy first layer, then next then next
                                    if (players[0].playerSlotType[i1 + 2] == "shield" && players[0].playerSlotHealth[i1 + 2] > 0 && players[0].playerSlotPowered[i1 + 2] > 0)
                                    {
                                        // in progress !!!
                                        resultShieldCapasityAfterDamage = players[0].playerSlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                        if (resultShieldCapasityAfterDamage <= 0)
                                        {
                                            players[0].playerSlotShieldCurrentCapacity[i1 + 2] = 0;
                                            damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                        }
                                        else
                                        {
                                            players[0].playerSlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                            damageToShip = 0;
                                        }
                                    }
                                }


                                players[0].playerShipCurrentHealth -= damageToShip / 2;
                                players[0].playerSlotHealth[players[1].playerWeaponSlotProjectileAimModule[i]] -= damageToShip / 2;

                              
                                //---
                                if (players[0].playerSlotHealth[players[1].playerWeaponSlotProjectileAimModule[i]] <= 0) 
                                {
                                    players[0].playerSlotHealth[players[1].playerWeaponSlotProjectileAimModule[i]] = 0;
                                    players[0].playerShipCurrentHealth -= damageToShip / 2;

                                    if (players[0].playerSlotPowered[players[1].playerWeaponSlotProjectileAimModule[i]] > 0)
                                    {
                                        players[0].playerSlotPowered[players[1].playerWeaponSlotProjectileAimModule[i]] = 0;
                                        players[0].playerShipFreeEnergy += 1;

                                        if (players[0].playerSlotType[players[1].playerWeaponSlotProjectileAimModule[i]] == "weaponcontrol")
                                        {
                                            for (int iii = 0; iii < players[0].playerWeaponSlotPowered.Length; iii++)
                                            {
                                                if (players[0].playerWeaponSlotPowered[iii] > 0)
                                                {
                                                    players[0].playerShipFreeEnergy += 1;
                                                    players[0].playerWeaponSlotPowered[iii] = 0;
                                                }
                                            }  
                                        }
                                    }
                                }

                                damageToShip = 0;
                            }
                            // if missed
                            //-- nothing(?)

                            players[1].playerWeaponSlotProjectileTime1[i,ii] = -1;
                        }
                    }
                }
                
            }
            //---

        }






        // ENERGY manipulation
        public void PlayerModuleEnergyUp(int moduleSlotId, int idInArray)
        {
            if (players[idInArray].playerSlotExist[moduleSlotId] != 0 && players[idInArray].playerSlotExist[moduleSlotId] != -1 && players[idInArray].playerSlotPowered[moduleSlotId] == 0 && players[idInArray].playerShipFreeEnergy <= players[idInArray].playerShipMaxEnergy && players[idInArray].playerSlotHealth[moduleSlotId] > 0)
            {
                players[idInArray].playerSlotPowered[moduleSlotId] = 1;
                players[idInArray].playerShipFreeEnergy -= 1;
            }
        }

        public void PlayerModuleEnergyDown(int moduleSlotId, int idInArray)
        {
            if (players[idInArray].playerSlotExist[moduleSlotId] != 0 && players[idInArray].playerSlotExist[moduleSlotId] != -1 && players[idInArray].playerSlotPowered[moduleSlotId] != 0 && players[idInArray].playerShipFreeEnergy >= 0 && players[idInArray].playerSlotHealth[moduleSlotId] > 0)
            {
                players[idInArray].playerSlotPowered[moduleSlotId] = 0;
                players[idInArray].playerShipFreeEnergy += 1;

                // unpower all weapons if weapon control was unpowered
                if (players[idInArray].playerSlotType[moduleSlotId] == "weaponcontrol") 
                {
                    for (int i = 0; i < players[idInArray].playerWeaponSlotPowered.Length; i++)
                    {
                        if (players[idInArray].playerWeaponSlotPowered[i] > 0)
                        {
                            players[idInArray].playerWeaponSlotPowered[i] = 0;
                            players[idInArray].playerShipFreeEnergy += 1;

                            players[idInArray].playerWeaponSlotCurrentReloadTime[i] = players[idInArray].playerWeaponSlotReloadTime[i];
                        }
                    }
                }
            }
        }


        public void PlayerWeaponEnergyUp(int weaponSlotId, int idInArray)
        {
            bool weaponControlPowered = false;
            // check if weapon control powered - if not - power first
            for (int i = 0; i < players[idInArray].playerSlotExist.Length; i++)
            {
                if (players[idInArray].playerSlotType[i] == "weaponcontrol" && players[idInArray].playerSlotPowered[i] <= 0 && players[idInArray].playerSlotHealth[i] > 0 && players[idInArray].playerShipFreeEnergy <= players[idInArray].playerShipMaxEnergy)
                {
                    players[idInArray].playerSlotPowered[i] += 1;
                    players[idInArray].playerShipFreeEnergy -= 1;
                }
                if (players[idInArray].playerSlotType[i] == "weaponcontrol" && players[idInArray].playerSlotPowered[i] > 0 && players[idInArray].playerSlotHealth[i] > 0) 
                {
                    weaponControlPowered = true;
                }
            }

            // power weapon
            if (weaponControlPowered == true && players[idInArray].playerWeaponSlotExist[weaponSlotId] != 0 && players[idInArray].playerWeaponSlotExist[weaponSlotId] != -1 && players[idInArray].playerWeaponSlotPowered[weaponSlotId] == 0 && players[idInArray].playerShipFreeEnergy <= players[idInArray].playerShipMaxEnergy)
            {
                players[idInArray].playerWeaponSlotPowered[weaponSlotId] = 1;
                players[idInArray].playerShipFreeEnergy -= 1;

                weaponControlPowered = false;
            }
            
        }

        public void PlayerWeaponEnergyDown(int weaponSlotId, int idInArray)
        {
            if (players[idInArray].playerWeaponSlotExist[weaponSlotId] != 0 && players[idInArray].playerWeaponSlotExist[weaponSlotId] != -1 && players[idInArray].playerWeaponSlotPowered[weaponSlotId] != 0 && players[idInArray].playerShipFreeEnergy >= 0)
            {
                players[idInArray].playerWeaponSlotPowered[weaponSlotId] = 0;
                players[idInArray].playerShipFreeEnergy += 1;

                players[idInArray].playerWeaponSlotCurrentReloadTime[weaponSlotId] = players[idInArray].playerWeaponSlotReloadTime[weaponSlotId];
            }
        }


        public void PlayerSetFocusTarget(int idInArray, int targetIdInArray)
        {
            players[idInArray].playerFocus = targetIdInArray;
        }

        // Player set destination point
        public void PlayerSetDestinationPointToMove(string moveToCoordinates, int idInArray) 
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            String[] coordinates = moveToCoordinates.Split(", ");
            double coordinateX = Convert.ToDouble(coordinates[0]);
            double coordinateY = Convert.ToDouble(coordinates[2]);

            players[idInArray].playerDestinationPositionX = coordinateX;
            players[idInArray].playerDestinationPositionY = coordinateY;
        }

        // move player to the point
        public void PlayerMovement() 
        {
            for (int i = 0; i < players.Count; i++)
            {
            // if engine active and not destroyed and exist
            if (players[i].playerSlotExist[0] != 0 && players[i].playerSlotExist[0] != -1 && players[i].playerSlotPowered[0] != 0 && players[i].playerSlotHealth[0] > 0)
            {

                int playerSpeed = players[i].playerShipMaxSpeed;



                // the most retardest way to get the next move point
                // calculate trajectory and move
                double distanceToPoint = Math.Sqrt(Math.Pow(players[i].playerDestinationPositionX - players[i].playerPositionX,2) + Math.Pow(players[i].playerDestinationPositionY - players[i].playerPositionY,2));
                    
                double[] newDistanceToPoint = new double[8];
                double[] newX = new double[8];
                double[] newY = new double[8];

                if (distanceToPoint > 0.1)
                {
                // ROTATION
                double rotationSpeed = 2;

                double xDiff = players[i].playerDestinationPositionX - players[i].playerPositionX;
                double yDiff = players[i].playerDestinationPositionY - players[i].playerPositionY;

                double c = yDiff;
                double b = xDiff;
                double a = Math.Sqrt(Math.Pow(b, 2) + Math.Pow(c, 2));
                double destinationDegrees = (180 / Math.PI) * (Math.Acos((Math.Pow(a, 2) + Math.Pow(c, 2) - Math.Pow(b, 2)) / (2 * a * c)));


                //if (xDiff > 0 && yDiff > 0) // first quarter 
                //{
                //    //   playerPositionRotation = destinationDegrees;
                //    Console.WriteLine("FIRST");
                //}
                //else if (xDiff == 0 && yDiff > 0) // up
                //{
                //    //   playerPositionRotation = 0;
                //    Console.WriteLine("UP");
                //}
                if (xDiff < 0 && yDiff > 0 && destinationDegrees > 0) // second quarter 
                {
                    //  playerPositionRotation = -destinationDegrees;
                    destinationDegrees = -destinationDegrees;
                    //Console.WriteLine("SECOND");

                }
                //else if (xDiff < 0 && yDiff == 0) // left  
                //{
                //    // playerPositionRotation = -90;
                //    Console.WriteLine("LEFT");
                //}
                else if (xDiff < 0 && yDiff < 0 && destinationDegrees > 0) // third quarter 
                {
                    //  playerPositionRotation = -destinationDegrees;
                    destinationDegrees = -destinationDegrees;
                    //Console.WriteLine("THIRD");
                }
                //else if (xDiff == 0 && yDiff < 0) // down  
                //{
                //    //   playerPositionRotation = 180;
                //    Console.WriteLine("DOWN");
                //}
                //else if (xDiff > 0 && yDiff < 0) // fourth quarter 
                //{
                //    //  playerPositionRotation = destinationDegrees;
                //    //Console.WriteLine("fOURTH");
                //}
                //else if (xDiff > 0 && yDiff == 0) // right  
                //{
                //    //  playerPositionRotation = 90;
                //    //Console.WriteLine("RIGHT");
                //}

              //  Console.WriteLine("destinationDegrees= " + destinationDegrees);
              //  Console.WriteLine("playerPositionRotation= " + playerPositionRotation);

                if (players[i].playerPositionRotation != destinationDegrees)
                {
                    if (destinationDegrees >= 0 && players[i].playerPositionRotation > (destinationDegrees - 180) && players[i].playerPositionRotation < destinationDegrees)
                    {
                                players[i].playerPositionRotation += rotationSpeed;
                    }
                    else if (destinationDegrees >= 0 && (players[i].playerPositionRotation < (destinationDegrees - 180) || players[i].playerPositionRotation > destinationDegrees))
                    {
                                players[i].playerPositionRotation -= rotationSpeed;
                    }
                    else if (destinationDegrees < 0 && players[i].playerPositionRotation < (destinationDegrees + 180) && players[i].playerPositionRotation > destinationDegrees)
                    {
                                players[i].playerPositionRotation -= rotationSpeed;
                    }
                    else if (destinationDegrees < 0 && (players[i].playerPositionRotation > (destinationDegrees + 180) || players[i].playerPositionRotation < destinationDegrees))
                    {
                                players[i].playerPositionRotation += rotationSpeed;
                    }


                    if (players[i].playerPositionRotation > 180) { players[i].playerPositionRotation = -179.999; }
                    if (players[i].playerPositionRotation < -180) { players[i].playerPositionRotation = 179.999; }

                    if (players[i].playerPositionRotation >= destinationDegrees - 1 && players[i].playerPositionRotation <= destinationDegrees + 1)
                    {
                                players[i].playerPositionRotation = destinationDegrees;
                    }
                }



                // impulse vector
                //double c = 10;
                //double A = 2;
                //double C = 90;

                //double B = 180 - A - C;

                //double a = c * Math.Cos((A * (Math.PI)) / 180);  // a = c·sin(A)/sin(C) = 7.07107 = 5√2
                //double b = c * Math.Cos((B * (Math.PI)) / 180); //b = c·sin(B)/sin(C) = 7.07107 = 5√2

                //Console.WriteLine("a = " + a);
                //Console.WriteLine("b = " + b);

                if (players[i].playerPositionRotation == 0) // up
                {
                            players[i].playerPositionY += playerSpeed;
                }
                else if (players[i].playerPositionRotation > 0 && players[i].playerPositionRotation < 90) // first quarter 
                {
                            players[i].playerPositionY += playerSpeed * Math.Cos((players[i].playerPositionRotation * (Math.PI)) / 180);
                            players[i].playerPositionX += playerSpeed * Math.Cos(((180 - players[i].playerPositionRotation - 90) * (Math.PI)) / 180);
                }
                else if (players[i].playerPositionRotation == 90) // right
                {
                            players[i].playerPositionX += playerSpeed;
                }
                else if (players[i].playerPositionRotation < 0 && players[i].playerPositionRotation > -90) // second quarter 
                {
                            players[i].playerPositionY += playerSpeed * Math.Cos(((-players[i].playerPositionRotation) * (Math.PI)) / 180);
                            players[i].playerPositionX -= playerSpeed * Math.Cos(((180 - (-players[i].playerPositionRotation) - 90) * (Math.PI)) / 180);
                }
                else if (players[i].playerPositionRotation == 180 || players[i].playerPositionRotation == -180) // down
                {
                            players[i].playerPositionY -= playerSpeed;
                }
                else if (players[i].playerPositionRotation > 90 && players[i].playerPositionRotation < 180) // fourth quarter 
                {
                            players[i].playerPositionY -= playerSpeed * Math.Cos(((180 - players[i].playerPositionRotation) * (Math.PI)) / 180);
                            players[i].playerPositionX += playerSpeed * Math.Cos(((180 - (180 - players[i].playerPositionRotation) - 90) * (Math.PI)) / 180);
                }
                else if (players[i].playerPositionRotation == -90) // left
                {
                            players[i].playerPositionX -= playerSpeed;
                }



                else if (players[i].playerPositionRotation < -90 && players[i].playerPositionRotation > -180) // third quarter 
                {
                            players[i].playerPositionY -= playerSpeed * Math.Cos(((180 + players[i].playerPositionRotation) * (Math.PI)) / 180);
                            players[i].playerPositionX -= playerSpeed * Math.Cos(((180 - (180 + players[i].playerPositionRotation) - 90) * (Math.PI)) / 180);
                }
                    }
                }
            }
        }


        public void AIDesctinationPointToMove() 
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerType == 1)
                {
                    players[i].playerDestinationPositionX = players[0].playerPositionX;
                    players[i].playerDestinationPositionY = players[0].playerPositionY;
                }
            }
        }

        //----------------------------------------
        //----------------------------------------



        // CLASS SYSTEM

        public List<PlayerBattleInformation> players { get; set; } = new List<PlayerBattleInformation>();

        public int[] playersToSet;
        public int[] playersTeamsToSet;

        public int mapId;




        //Variables OLD
        public int battleId { get; set; }

        public int toStart { get; set; }
        public int started { get; set; }

    //    public int playerReady { get; set; } // DELETE
        public int finished { get; set; }
        public int battleTime { get; set; } // in ms


    }








    /*
         PlayerBattleInformation - class for battle, contain all information about the battle
           and all actions that can be in that battle---------------
       */

    public class PlayerBattleInformation
    {
        public PlayerBattleInformation()
        {
            playerId = 0;

            playerReady = 0;


            // focus
            playerFocus = 0;

            playerVisionRadius = 250;
            playerShipMaxSpeed = 5;

        }

        public int playerType { get; set; } // 0 - Human, 1 - AI
        public int playerId { get; set; } //ID of the player in DB or ID of the AI in the DB depends on the playerType
        public int playerTeam { get; set; } // team of the players 

        public int playerReady { get; set; } // if AI - should be set ready immidiately, if player - set appropriately when player is ready

        public int playerShipId { get; set; } // ship ID // ???????? is it different for AI and player??


        // NEW SYSTEM

        // position on the map

        public double playerPositionX { get; set; }
        public double playerPositionY { get; set; }
        public double playerPositionRotation { get; set; }

        // destination point

        public double playerDestinationPositionX { get; set; }
        public double playerDestinationPositionY { get; set; }
        public double playerDestinationPositionRotation { get; set; }

        // current focus of the player 
        public int playerFocus { get; set; }

        // player vision radious 

        public double playerVisionRadius { get; set; }

        // ship

        public int playerShipMaxHealth { get; set; }
        public int playerShipCurrentHealth { get; set; }
        public int playerShipMaxEnergy { get; set; }
        public int playerShipFreeEnergy { get; set; }

        public int playerShipMaxSpeed { get; set; }


        // sum shield

        public int playerSumShieldCapacity { get; set; }
        public int playerSumShieldCurrentCapacity { get; set; } = 0;

        // p modules

        //[ engine , cockpit, biglot1 .. 5, mediumslot 1 .. 5, smallslot 1 .. 5]

        public int[] playerSlotExist { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotHealth { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotPowered { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotEnergyRequired { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public string[] playerSlotType { get; set; } = new string[17] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", " 0", "0", "0" };

        //bigslot difference slots
        public int[] playerSlotAdditionalInfoToClient { get; set; } = new int[17] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }; // - -1 = no additional info
        public int[] playerSlotShieldCapacity { get; set; } = new int[17];

        public int[] playerSlotShieldCurrentCapacity { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotShieldRechargeTime { get; set; } = new int[17];
        public int[] playerSlotShieldRechargeCurrentTime { get; set; } = new int[17];
        public int[] playerSlotShieldRechargeRate { get; set; } = new int[17];

        public int[] playerSlotWeaponControlAmountOfWeapons { get; set; } = new int[17];

        // weapons

        public int[] playerWeaponSlotExist { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotPowered { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotEnergyRequired { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotDamage { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotReloadTime { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotCurrentReloadTime { get; set; } = new int[5] { 0, 0, 0, 0, 0 };

        public int[] playerWeaponSlotProjectileTime { get; set; } = new int[5] { -1, -1, -1, -1, -1 }; // -1 = does not exist, 0 - hit by time, N- time
        public int[] playerWeaponSlotProjectileAimModule { get; set; } = new int[5] { 0, 0, 0, 0, 0 };

        //test // -1 = does not exist, 0 - hit by time, N- time
        public int[,] playerWeaponSlotProjectileTime1 { get; set; } = new int[5, 5] { { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 } };

        public int[] playerWeaponSlotProjectileAimModule1 { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        //------------------------

        // Crew

        public int[] playerCrewExist { get; set; }
        public int[] playerCrewHealth { get; set; }
        public int[] playerCrewDamage { get; set; }

    }

}
