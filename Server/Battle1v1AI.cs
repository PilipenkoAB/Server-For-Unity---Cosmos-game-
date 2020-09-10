using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server
{
    /*
         Battle1v1AI - class for battle, contain all information about the battle
           and all actions that can be in that battle---------------
       */
    public class Battle1v1AI
    {

        // Constructor that takes - starting point no arguments:
        public Battle1v1AI()
        {
            toStart = 0;
            started = 0;
            finished = 0;
            battleTime = 0;
            playerReady = 0;
            // Calculatet starting parameters after getting them at creating the class

        }

        public void SetStartHealth()
        {
            playerShipCurrentHealth = playerShipMaxHealth;
            aIShipCurrentHealth = aIShipMaxHealth;
        }

        public void SetStartReload()
        {
            for (int i = 0; i < 5; i++)
            {
                if (playerWeaponSlotExist[i] == 1) 
                {
                    playerWeaponSlotCurrentReloadTime[i] = playerWeaponSlotReloadTime[i];
                }
                if (aIWeaponSlotExist[i] == 1)
                {
                    aIWeaponSlotCurrentReloadTime[i] = aIWeaponSlotReloadTime[i];
                }
            }
        }


        public void ReloadAllWeaponsPerTick()
        {
            int reloadOneTick = 50; // ms

            // reload of the player weapon
            for (int i = 0; i < playerWeaponSlotCurrentReloadTime.Length; i++)
            {
                if (playerWeaponSlotCurrentReloadTime[i] > 0 && playerWeaponSlotPowered[i] > 0)
                {
                    playerWeaponSlotCurrentReloadTime[i] -= reloadOneTick;

                }
                else if (playerWeaponSlotCurrentReloadTime[i] <= 0)
                {
                    playerWeaponSlotCurrentReloadTime[i] = 0;
                }
            }

            // reload of the ai weapon
            for (int i = 0; i < aIWeaponSlotCurrentReloadTime.Length; i++)
            {
                if (aIWeaponSlotCurrentReloadTime[i] > 0 && aIWeaponSlotPowered[i] > 0)
                {
                    aIWeaponSlotCurrentReloadTime[i] -= reloadOneTick;
                }
                else if (aIWeaponSlotCurrentReloadTime[i] <= 0)
                {
                    aIWeaponSlotCurrentReloadTime[i] = 0;
                }
            }
        }


        public void ReloadAllShieldsPerTick() 
        {
            int reloadOneTick = 50; // ms

            int playerSumCapasity = 0;
            int aISumCapasity = 0;
            //player shields
            for (int i = 0; i < 5; i++) 
            {
                if (playerSlotExist[i + 2] != 0 && playerSlotExist[i + 2] != -1 && playerSlotType[i + 2] == "shield" && playerSlotPowered[i + 2] <= 0) 
                {
                    playerSlotShieldCurrentCapacity[i + 2] = 0;
                    playerSlotShieldRechargeCurrentTime[i + 2] = playerSlotShieldRechargeTime[i + 2];
                }
                else if (playerSlotExist[i + 2] != 0 && playerSlotExist[i + 2] != -1 && playerSlotType[i + 2] == "shield" && playerSlotPowered[i + 2] > 0 && playerSlotHealth[i + 2] > 0 && playerSlotShieldRechargeCurrentTime[i + 2] > 0)
                {
                  //  Console.WriteLine("DEBUG - recharge time before - " + playerSlotShieldRechargeCurrentTime[i + 2]);
                    playerSlotShieldRechargeCurrentTime[i + 2] -= reloadOneTick;
                  //  Console.WriteLine("DEBUG - recharge time after - " + playerSlotShieldRechargeCurrentTime[i + 2]);
                }
                else if(playerSlotShieldRechargeCurrentTime[i + 2] <= 0)
                {
                    playerSlotShieldCurrentCapacity[i + 2] += playerSlotShieldRechargeRate[i + 2];

                    if (playerSlotShieldCurrentCapacity[i + 2] > playerSlotShieldCapacity[i + 2])
                    {
                        playerSlotShieldCurrentCapacity[i + 2] = playerSlotShieldCapacity[i + 2];
                    }

                    playerSlotShieldRechargeCurrentTime[i + 2] = playerSlotShieldRechargeTime[i + 2];
                   // Console.WriteLine("DEBUG - playerSlotShieldRechargeTime[i + 2] - " + playerSlotShieldRechargeTime[i + 2]);

                }

                playerSumCapasity += playerSlotShieldCurrentCapacity[i + 2];
                // sum capacity
                playerSumShieldCurrentCapacity = playerSumCapasity;
            }

            //ai shields
            for (int i = 0; i < 5; i++)
            {
                if (aISlotExist[i + 2] != 0 && aISlotExist[i + 2] != -1 && aISlotType[i + 2] == "shield" && aISlotPowered[i + 2] <= 0)
                {
                    aISlotShieldCurrentCapacity[i + 2] = 0;
                    aISlotShieldRechargeCurrentTime[i + 2] = aISlotShieldRechargeTime[i + 2];
                }
                else if (aISlotExist[i + 2] != 0 && aISlotExist[i + 2] != -1 && aISlotType[i + 2] == "shield" && aISlotPowered[i + 2] > 0 && aISlotHealth[i + 2] > 0 && aISlotShieldRechargeCurrentTime[i + 2] > 0)
                {
                    //  Console.WriteLine("DEBUG - recharge time before - " + playerSlotShieldRechargeCurrentTime[i + 2]);
                    aISlotShieldRechargeCurrentTime[i + 2] -= reloadOneTick;
                    //  Console.WriteLine("DEBUG - recharge time after - " + playerSlotShieldRechargeCurrentTime[i + 2]);
                }
                else if (aISlotShieldRechargeCurrentTime[i + 2] <= 0)
                {
                    aISlotShieldCurrentCapacity[i + 2] += aISlotShieldRechargeRate[i + 2];

                    if (aISlotShieldCurrentCapacity[i + 2] > aISlotShieldCapacity[i + 2])
                    {
                        aISlotShieldCurrentCapacity[i + 2] = aISlotShieldCapacity[i + 2];
                    }

                    aISlotShieldRechargeCurrentTime[i + 2] = aISlotShieldRechargeTime[i + 2];
                    // Console.WriteLine("DEBUG - playerSlotShieldRechargeTime[i + 2] - " + playerSlotShieldRechargeTime[i + 2]);

                }

                aISumCapasity += aISlotShieldCurrentCapacity[i + 2];
                // sum capacity
                aISumShieldCurrentCapacity = aISumCapasity;
            }


          //  Console.WriteLine("DEBUG playerSumCapasity - " + playerSumCapasity);
         //  Console.WriteLine("DEBUG aISumCapasity - " + aISumCapasity);
        }

        //-----------------------

        public void AIAttackAllWeaponsCooldown() 
        {
            for (int i = 0; i < aIWeaponSlotCurrentReloadTime.Length; i++)
            {
                if (aIWeaponSlotCurrentReloadTime[i] == 0 && aISlotType[i] == "weaponcontrol" && aISlotPowered[i] == 1)
                {
                    //    Console.WriteLine("DEBUG weapon - "+i);
                    aIWeaponSlotProjectileTime[i, 0] = 1500; // need to set if how many projectles ?????
                    aIWeaponSlotCurrentReloadTime[i] = aIWeaponSlotReloadTime[i];

                    //get random slot to attack
                    Random randomSlotToAttack = new Random();
                    int slotToAttack = randomSlotToAttack.Next(0,11); // 11 because not small slots to attack
                    while (playerSlotExist[slotToAttack] < 0) 
                    {
                        slotToAttack = randomSlotToAttack.Next(0, 11);
                    //    Console.WriteLine("DEBUG RANDOM try - " + slotToAttack);
                    }
                  //  Console.WriteLine("DEBUG RANDOM attack - " + slotToAttack);
                    aIWeaponSlotProjectileAimModule[i] = slotToAttack;
                }
            }
        }


        public void AIPowerModules() 
        {
            for (int i = 0; i < aISlotExist.Length; i++)
            {
                if (aISlotExist[i] > 0 && aISlotPowered[i] != 1 && aISlotHealth[i] > 0 && aIShipFreeEnergy > 0)
                {
                    aISlotPowered[i] = 1;
                    aIShipFreeEnergy -= 1;
                }
            }

            for (int i = 0; i < aIWeaponSlotExist.Length; i++)
            {
                if (aIWeaponSlotExist[i] > 0 && aIWeaponSlotPowered[i] != 1 && aIShipFreeEnergy > 0)
                {
                    aIWeaponSlotPowered[i] = 1;
                    aIShipFreeEnergy -= 1;
                }
            }
        }
        //----------------


        public bool PlayerAttackModule(int weaponIdint, int moduleSlotId) 
        {
            // player attack AI
            // Console.WriteLine("DEBUG - weaponIdint - " + weaponIdint);
            //if weapon control exist and powered
            
            for (int i = 0; i < playerSlotType.Length; i++)
            {
               // Console.WriteLine("DEBUG i -"+i+ " playerSlotType[i] -"+ playerSlotType[i]);

                if (playerWeaponSlotCurrentReloadTime[weaponIdint] == 0 && playerSlotType[i] == "weaponcontrol" && playerSlotPowered[i] == 1 && playerWeaponSlotPowered[weaponIdint] == 1)
                {
                //    Console.WriteLine("DEBUG weapon - "+i);
                    playerWeaponSlotProjectileTime[weaponIdint] = 1500;
                    playerWeaponSlotCurrentReloadTime[weaponIdint] = playerWeaponSlotReloadTime[weaponIdint];
                    playerWeaponSlotProjectileAimModule[weaponIdint] = moduleSlotId;
                    // shot is happened
                    return true;
                }

            }

            return false;
        }

        public void ProjectilesMoveTime() 
        {
            int reloadOneTick = 50; // ms

            for (int i = 0; i < playerWeaponSlotProjectileTime.Length; i++)
            {
                if (playerWeaponSlotProjectileTime[i] > 0)
                {

                    playerWeaponSlotProjectileTime[i] -= reloadOneTick;

                   // Console.WriteLine("DEBUG projectile before - i "+i+" - " + playerWeaponSlotProjectileTime[i]);

                    if (playerWeaponSlotProjectileTime[i] <= 0) 
                    {
                        // projectile hit the enemyAI
                        Random randomChanceToHit = new Random();
                        int ChanceToHit = randomChanceToHit.Next(0, 100);

                        Random randomDamage = new Random();
                        int damageToShip = randomDamage.Next(Convert.ToInt32(Convert.ToDouble(playerWeaponSlotDamage[i]) * 0.9), Convert.ToInt32(Convert.ToDouble(playerWeaponSlotDamage[i]) * 1.1));
                        int resultShieldCapasityAfterDamage = 0;

                        if (ChanceToHit <= 50) // if hit the ship but not module
                        {
                            for (int i1 = 0; i1 < 5; i1++)
                            {
                                // layers of the shields. first - destroy first layer, then next then next
                                if (aISlotType[i1 + 2] == "shield" && aISlotHealth[i1 + 2] > 0 && aISlotPowered[i1 + 2] > 0)
                                {
                                    // in progress !!!
                                    resultShieldCapasityAfterDamage = aISlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                    if (resultShieldCapasityAfterDamage <= 0)
                                    {
                                        aISlotShieldCurrentCapacity[i1 + 2] = 0;
                                        damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                    }
                                    else
                                    {
                                        aISlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                        damageToShip = 0;
                                    }
                                }
                            }

                            // damage to ship if no shields (all damage in 1 take)
                            aIShipCurrentHealth -= damageToShip;
                            damageToShip = 0;
                        } 
                        else if(ChanceToHit > 50 && ChanceToHit <= 75) // if hit the module (half damage to ship, half damage to module
                        {
                            for (int i1 = 0; i1 < 5; i1++)
                            {
                                // layers of the shields. first - destroy first layer, then next then next
                                if (aISlotType[i1 + 2] == "shield" && aISlotHealth[i1 + 2] > 0 && aISlotPowered[i1 + 2] > 0)
                                {
                                    // in progress !!!
                                    resultShieldCapasityAfterDamage = aISlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                    if (resultShieldCapasityAfterDamage <= 0)
                                    {
                                        aISlotShieldCurrentCapacity[i1 + 2] = 0;
                                        damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                    }
                                    else
                                    {
                                        aISlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                        damageToShip = 0;
                                    }
                                }
                            }

                            aIShipCurrentHealth -= damageToShip / 2;
                            aISlotHealth[playerWeaponSlotProjectileAimModule[i]] -= damageToShip / 2;

                            if (aISlotHealth[playerWeaponSlotProjectileAimModule[i]] <= 0)
                            {
                                aIShipCurrentHealth -= damageToShip / 2; // aditional damage if module was destroyed

                                aISlotHealth[playerWeaponSlotProjectileAimModule[i]] = 0;
                                aISlotPowered[playerWeaponSlotProjectileAimModule[i]] = 0;

                                if (aISlotPowered[playerWeaponSlotProjectileAimModule[i]] > 0)
                                {
                                    aISlotPowered[playerWeaponSlotProjectileAimModule[i]] = 0;
                                    aIShipFreeEnergy += 1;

                                    if (aISlotType[playerWeaponSlotProjectileAimModule[i]] == "weaponcontrol")
                                    {
                                        for (int iii = 0; iii < aIWeaponSlotPowered.Length; iii++)
                                        {
                                            if (aIWeaponSlotPowered[iii] > 0)
                                            {
                                                aIShipFreeEnergy += 1;
                                                aIWeaponSlotPowered[iii] = 0;
                                            }
                                        }
                                    }
                                }
                            }

                            damageToShip = 0;
                        }
                        // if missed
                        //-- nothing(?)


                        // Console.WriteLine("DEBUG projectile after - " + playerWeaponSlotProjectileTime[i]);

                        playerWeaponSlotProjectileTime[i] = -1; 
                    }
                }
            }


            //ai (multiple projectiles + shields)
            for (int i = 0; i < 5; i++) //weapon N
            {
                for (int ii = 0; ii < 5; ii++) //projectile N
                {
                    if (aIWeaponSlotProjectileTime[i,ii] > 0)
                    {

                        aIWeaponSlotProjectileTime[i,ii] -= reloadOneTick;

                       // Console.WriteLine("DEBUG ai projectile before - i " + i + " - " + aIWeaponSlotProjectileTime[i,ii]);

                        if (aIWeaponSlotProjectileTime[i,ii] <= 0)
                        {
                            // projectile hit the enemyAI
                            Random randomChanceToHit = new Random();
                            int ChanceToHit = randomChanceToHit.Next(0, 100);

                            Random randomDamage = new Random();
                            int damageToShip = randomDamage.Next(Convert.ToInt32(Convert.ToDouble(aIWeaponSlotDamage[i]) * 0.9), Convert.ToInt32(Convert.ToDouble(aIWeaponSlotDamage[i]) * 1.1));
                            int resultShieldCapasityAfterDamage = 0;

                            if (ChanceToHit <= 50) // if hit the ship but not module
                            {
                               // Console.WriteLine("DEBUG HIT SHIP");
                                // damage with shields

                                for (int i1 = 0; i1 < 5; i1++)
                                {
                                    // layers of the shields. first - destroy first layer, then next then next
                                    if (playerSlotType[i1 + 2] == "shield" && playerSlotHealth[i1 + 2] > 0 && playerSlotPowered[i1 + 2] > 0)
                                    {
                                        // in progress !!!
                                        resultShieldCapasityAfterDamage = playerSlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                        if (resultShieldCapasityAfterDamage <= 0)
                                        {
                                            playerSlotShieldCurrentCapacity[i1 + 2] = 0;
                                            damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                        }
                                        else
                                        {
                                            playerSlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                            damageToShip = 0;
                                        }
                                    }
                                }

                                // damage to ship if no shields (all damage in 1 take)
                                playerShipCurrentHealth -= damageToShip;
                                damageToShip = 0;

                               // Console.WriteLine("damage = " + damageToShip);
                               // Console.WriteLine("shield = "+ playerSumShieldCurrentCapacity);
                               // Console.WriteLine("health = " + playerShipCurrentHealth);


                            }
                            else if (ChanceToHit > 50 && ChanceToHit <= 75) // if hit the module (half damage to ship, half damage to module
                            {
                              //  Console.WriteLine("DEBUG HIT MODULE - " + damageToShip);
                                for (int i1 = 0; i1 < 5; i1++)
                                {
                                    // layers of the shields. first - destroy first layer, then next then next
                                    if (playerSlotType[i1 + 2] == "shield" && playerSlotHealth[i1 + 2] > 0 && playerSlotPowered[i1 + 2] > 0)
                                    {
                                        // in progress !!!
                                        resultShieldCapasityAfterDamage = playerSlotShieldCurrentCapacity[i1 + 2] - damageToShip;
                                        if (resultShieldCapasityAfterDamage <= 0)
                                        {
                                            playerSlotShieldCurrentCapacity[i1 + 2] = 0;
                                            damageToShip = Math.Abs(resultShieldCapasityAfterDamage); // set damage for be reduced in next shield layer
                                        }
                                        else
                                        {
                                            playerSlotShieldCurrentCapacity[i1 + 2] -= damageToShip;
                                            damageToShip = 0;
                                        }
                                    }
                                }


                                playerShipCurrentHealth -= damageToShip / 2;
                                playerSlotHealth[aIWeaponSlotProjectileAimModule[i]] -= damageToShip / 2;

                              
                                //---
                                if (playerSlotHealth[aIWeaponSlotProjectileAimModule[i]] <= 0) 
                                {
                                    playerSlotHealth[aIWeaponSlotProjectileAimModule[i]] = 0;
                                    playerShipCurrentHealth -= damageToShip / 2;

                                    if (playerSlotPowered[aIWeaponSlotProjectileAimModule[i]] > 0)
                                    {
                                        playerSlotPowered[aIWeaponSlotProjectileAimModule[i]] = 0;
                                        playerShipFreeEnergy += 1;

                                        if (playerSlotType[aIWeaponSlotProjectileAimModule[i]] == "weaponcontrol")
                                        {
                                            for (int iii = 0; iii < playerWeaponSlotPowered.Length; iii++)
                                            {
                                                if (playerWeaponSlotPowered[iii] > 0)
                                                {
                                                    playerShipFreeEnergy += 1;
                                                    playerWeaponSlotPowered[iii] = 0;
                                                }
                                            }  
                                        }
                                    }
                                }

                                damageToShip = 0;
                                //----
                                //  Console.WriteLine("slot - "+ aIWeaponSlotProjectileAimModule[i] + " - "+playerSlotHealth[aIWeaponSlotProjectileAimModule[i]]);
                            }
                            // if missed
                            //-- nothing(?)

                           // Console.WriteLine("DEBUG ai projectile after - " + aIWeaponSlotProjectileTime[i,ii]);

                            aIWeaponSlotProjectileTime[i,ii] = -1;
                        }
                    }
                }
                
            }
            //---

        }



        // ENERGY manipulation
        public void PlayerModuleEnergyUp(int moduleSlotId)
        {
            if (playerSlotExist[moduleSlotId] != 0 && playerSlotExist[moduleSlotId] != -1 && playerSlotPowered[moduleSlotId] == 0 && playerShipFreeEnergy <= playerShipMaxEnergy && playerSlotHealth[moduleSlotId] > 0)
            {
                playerSlotPowered[moduleSlotId] = 1;
                playerShipFreeEnergy -= 1;
            }
        }

        public void PlayerModuleEnergyDown(int moduleSlotId)
        {
            if (playerSlotExist[moduleSlotId] != 0 && playerSlotExist[moduleSlotId] != -1 && playerSlotPowered[moduleSlotId] != 0 && playerShipFreeEnergy >= 0 && playerSlotHealth[moduleSlotId] > 0)
            {
                playerSlotPowered[moduleSlotId] = 0;
                playerShipFreeEnergy += 1;

                // unpower all weapons if weapon control was unpowered
                if (playerSlotType[moduleSlotId] == "weaponcontrol") 
                {
                    for (int i = 0; i < playerWeaponSlotPowered.Length; i++)
                    {
                        if (playerWeaponSlotPowered[i] > 0)
                        {
                            playerWeaponSlotPowered[i] = 0;
                            playerShipFreeEnergy += 1;

                            playerWeaponSlotCurrentReloadTime[i] = playerWeaponSlotReloadTime[i];
                        }
                    }
                }
            }
        }


        public void PlayerWeaponEnergyUp(int weaponSlotId)
        {
            bool weaponControlPowered = false;
            // check if weapon control powered - if not - power first
            for (int i = 0; i < playerSlotExist.Length; i++)
            {
                if (playerSlotType[i] == "weaponcontrol" && playerSlotPowered[i] <= 0 && playerSlotHealth[i] > 0 && playerShipFreeEnergy <= playerShipMaxEnergy)
                {
                   // Console.WriteLine("DEBUG health - " + playerSlotHealth[i]);
                    playerSlotPowered[i] += 1;
                    playerShipFreeEnergy -= 1;
                }
                if (playerSlotType[i] == "weaponcontrol" && playerSlotPowered[i] > 0 && playerSlotHealth[i] > 0) 
                {
                  //  Console.WriteLine("DEBUG health 1  - " + playerSlotHealth[i]);
                    weaponControlPowered = true;
                }
            }

            // power weapon
            if (weaponControlPowered == true && playerWeaponSlotExist[weaponSlotId] != 0 && playerWeaponSlotExist[weaponSlotId] != -1 && playerWeaponSlotPowered[weaponSlotId] == 0 && playerShipFreeEnergy <= playerShipMaxEnergy)
            {
                //Console.WriteLine("DEBUG health power" + weaponControlPowered);

               playerWeaponSlotPowered[weaponSlotId] = 1;
                playerShipFreeEnergy -= 1;

                weaponControlPowered = false;
            }
            
        }

        public void PlayerWeaponEnergyDown(int weaponSlotId)
        {
            if (playerWeaponSlotExist[weaponSlotId] != 0 && playerWeaponSlotExist[weaponSlotId] != -1 && playerWeaponSlotPowered[weaponSlotId] != 0 && playerShipFreeEnergy >= 0)
            {
                playerWeaponSlotPowered[weaponSlotId] = 0;
                playerShipFreeEnergy += 1;

                playerWeaponSlotCurrentReloadTime[weaponSlotId] = playerWeaponSlotReloadTime[weaponSlotId];
            }
        }
       
        
        
        
        
        //Variables
        public int battle1v1AIId { get; set; }

        public int toStart { get; set; }
        public int started { get; set; }

        public int playerReady { get; set; }
        public int finished { get; set; }
        public int battleTime { get; set; } // in ms

        // PLAYER and AI ID
        public int aiId { get; set; } // DO I NEED THIS???

        public int playerID { get; set; } // DO I NEED THIS???

        // player shipId and ai shipId
        public int playerShipId { get; set; }

        public int aIShipId { get; set; }


        ////-------------------- Player -------------------------

        // NEW SYSTEM

        // PLAYER

        // ship

        public int playerShipMaxHealth { get; set; }
        public int playerShipCurrentHealth { get; set; }
        public int playerShipMaxEnergy { get; set; }
        public int playerShipFreeEnergy { get; set; }

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

        public int[] playerWeaponSlotProjectileAimModule1 { get; set; } = new int[5] { 0,0,0,0,0 };
        //------------------------

        // Crew

        public int[] playerCrewExist { get; set; }
        public int[] playerCrewHealth { get; set; }
        public int[] playerCrewDamage { get; set; }



        // AI

        // ship

        public int aIShipMaxHealth { get; set; }
        public int aIShipCurrentHealth { get; set; }
        public int aIShipMaxEnergy { get; set; }
        public int aIShipFreeEnergy { get; set; }


        // sum shield

        public int aISumShieldCapacity { get; set; }
        public int aISumShieldCurrentCapacity { get; set; } = 0;

        // p modules

        //[ engine , cockpit, biglot1 .. 5, mediumslot 1 .. 5]

        public int[] aISlotExist { get; set; } = new int[17];
        public int[] aISlotHealth { get; set; } = new int[17];
        public int[] aISlotPowered { get; set; } = new int[17];
        public int[] aISlotEnergyRequired { get; set; } = new int[17];
        public string[] aISlotType { get; set; } = new string[17];

        //bigslot difference slots
        public int[] aISlotShieldCapacity { get; set; } = new int[17];
        public int[] aISlotShieldRechargeTime { get; set; } = new int[17];
        public int[] aISlotShieldRechargeCurrentTime { get; set; } = new int[17];
        public int[] aISlotShieldRechargeRate { get; set; } = new int[17];
        public int[] aISlotShieldCurrentCapacity { get; set; } = new int[17];

        public int[] aISlotWeaponControlAmountOfWeapons { get; set; } = new int[17];

        // weapons

        public int[] aIWeaponSlotExist { get; set; } = new int[5];
        public int[] aIWeaponSlotPowered { get; set; } = new int[5];
        public int[] aIWeaponSlotEnergyRequired { get; set; } = new int[5];
        public int[] aIWeaponSlotDamage { get; set; } = new int[5];
        public int[] aIWeaponSlotReloadTime { get; set; } = new int[5];
        public int[] aIWeaponSlotCurrentReloadTime { get; set; } = new int[5];

        //test // -1 = does not exist, 0 - hit by time, N- time
        public int[,] aIWeaponSlotProjectileTime { get; set; } = new int[5, 5] { { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 }, { -1, -1, -1, -1, -1 } };

        public int[] aIWeaponSlotProjectileAimModule { get; set; } = new int[5] { 0, 0, 0, 0, 0 };

        // Crew

        public int[] aICrewExist { get; set; }
        public int[] aICrewHealth { get; set; }
        public int[] aICrewDamage { get; set; }


    }





    /*
     Class - PlayerShip
     */
    //public class PlayerShip : Battle1v1AI
    //{
    //    public PlayerShip()
    //    {
    //    }

    //}

    ///*
    // Class - AiShip
    //*/
    //public class AIShip : Battle1v1AI
    //{
    //    public AIShip()
    //    {
    //    }

    //}

    /*
    Class - slot
    */
    //public class ModuleShipSlot
    //{
    //    public ModuleShipSlot()
    //    {
    //    }

    //  //  public int aIId { get; set; }
    //  //  public int aIShipId { get; set; }
    //}
}
