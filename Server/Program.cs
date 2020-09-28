/*
  Created by Aleksandr Pilipenko

  2020 year
*/


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

using System.Threading;
using System.Threading.Tasks;

using System.Data.SQLite;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Runtime.InteropServices;

namespace Server
{
    /*
      Main class that initiate TCP server and process all responses
    */
    class MainServer
    {
        // Define dictionary with classes of battles
        private static Dictionary<int, BattleSession> sessionsBattle1v1AI = new Dictionary<int, BattleSession>();

        // 04.09.2020 - Change Dictionary to the List?
        //  private static List<Battle1v1AI> sessionsBattle1v1AI = new List<Battle1v1AI>();

        // Define dictionary as cach for logined player
        // key - playerId  values - [sessionToken] \  [battle session]
        private static Dictionary<string, List<string>> playerCache = new Dictionary<string, List<string>>();

        // string connection to the Data Base
        private static string connectionToDBString = "Data Source = DB.db; Version = 3;";

        // port of the server - TCP listener on port 13000.
        private static int port = 13000;

        //BattleTickTime in ms
        public static int battleTickTime = 50;



        // Main 
        static void Main(string[] args)
        {
            // server      and battle
            //  TCPServer();


            Parallel.Invoke(TCPServer, BattleProcess);
            // test();
        }

        static public void BattleProcess()
        {
            //Update DB (closed = 1) - close battle in DB


            while (true)  // check dictionary with battle's all the time for if need to start battle or not
            {
                try
                {
                    // check if battle need to be started
                    foreach (int battleSessionId in sessionsBattle1v1AI.Keys)
                    { // next line correct if ALL players are ready
                        if (sessionsBattle1v1AI[battleSessionId].toStart == 1 && sessionsBattle1v1AI[battleSessionId].started == 0 && sessionsBattle1v1AI[battleSessionId].players[0].playerReady == 1)
                        {
                            // idea is -> if any player is not ready - > set that session is not ready to Start
                            bool sessionReadyToStart = true;
                            for (int i = 0; i < sessionsBattle1v1AI[battleSessionId].players.Count; i++)
                            {
                                if (sessionsBattle1v1AI[battleSessionId].players[i].playerReady != 1)
                                {
                                    sessionReadyToStart = false;
                                }
                            }

                            if (sessionReadyToStart == true) // if all players ready - > start session
                            {
                                // Process battle
                                sessionsBattle1v1AI[battleSessionId].started = 1;

                                Task.Run(() => {
                                    Console.WriteLine("battle session number - " + battleSessionId + " is started");



                                    // set start health
                                    sessionsBattle1v1AI[battleSessionId].SetStartHealth();
                                    sessionsBattle1v1AI[battleSessionId].SetStartReload();


                                    //update battle session status every 50ms
                                    while (sessionsBattle1v1AI[battleSessionId].finished != 1)
                                    {
                                        //  -------------------------- Battle process NEW -----------------------------
                                        // AI
                                        // try to power modules of all AI (for example, if module was unpowered by something
                                        for (int i = 0; i < sessionsBattle1v1AI[battleSessionId].players.Count; i++)
                                        {
                                            if (sessionsBattle1v1AI[battleSessionId].players[i].playerType == 1)
                                            {
                                                sessionsBattle1v1AI[battleSessionId].AIPowerModules(i);
                                            }
                                        }

                                        // set destination point to move (test - zero player - human player)
                                        sessionsBattle1v1AI[battleSessionId].AIDesctinationPointToMove();



                                        //  -------------------------- Battle process - OLD -----------------------------

                                        // RELOAD
                                        sessionsBattle1v1AI[battleSessionId].ReloadAllWeaponsPerTick();

                                        // sheilds reload , shield control if power is off 
                                        sessionsBattle1v1AI[battleSessionId].ReloadAllShieldsPerTick();


                                        // Set time of moving of projectile
                                        sessionsBattle1v1AI[battleSessionId].ProjectilesMoveTime();

                                        // Calculate movement trajectory for player
                                        sessionsBattle1v1AI[battleSessionId].PlayerMovement();

                                        // set focus update if out of vision range or something else - remove focus
                                        sessionsBattle1v1AI[battleSessionId].UpdateAllFocus();

                                        // AI
                                        // power modules (for example, if module was unpowered by something
                                        //sessionsBattle1v1AI[battleSessionId].AIPowerModules(1);
                                        // attack if no on cooldown
                                        sessionsBattle1v1AI[battleSessionId].AIAttackAllWeaponsCooldown(1);

                                        // check if someone dead
                                        if (sessionsBattle1v1AI[battleSessionId].players[0].playerShipCurrentHealth <= 0)
                                        {
                                            Console.WriteLine("player dead in  session number - " + battleSessionId + "  and it finished");
                                            sessionsBattle1v1AI[battleSessionId].finished = 1;
                                        }
                                        if (sessionsBattle1v1AI[battleSessionId].players[1].playerShipCurrentHealth <= 0)
                                        {
                                            Console.WriteLine("ai dead in  session number - " + battleSessionId + "  and it finished");
                                            sessionsBattle1v1AI[battleSessionId].finished = 1;
                                        }

                                        //------------------------- end of battle process --------------------------


                                        // check if time for battle session is up (600000 ms = 10 min)
                                        if (sessionsBattle1v1AI[battleSessionId].battleTime >= 600000)
                                        {
                                            sessionsBattle1v1AI[battleSessionId].finished = 1;
                                            Console.WriteLine("time for session number - " + battleSessionId + " is up and finished");
                                        }


                                        // update time  
                                        //Console.WriteLine(sessionsBattle1v1AI[battleSessionId].battleTime);
                                        sessionsBattle1v1AI[battleSessionId].battleTime += battleTickTime;
                                        Thread.Sleep(battleTickTime);

                                    }

                                    // if finished - update DB with results
                                    if (sessionsBattle1v1AI[battleSessionId].finished == 1)
                                    {
                                        Console.WriteLine("update DB after finishin in session " + battleSessionId);

                                        //Update DB (closed = 1) - close battle in DB
                                        string enqueryUpdate = "UPDATE Battle1v1Ai SET Closed = 1 WHERE Battle1v1AiId = @battleSessionId";

                                        using var connectionToDB = new SQLiteConnection(connectionToDBString);
                                        connectionToDB.Open();
                                        using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);
                                        commandUpdate.Parameters.AddWithValue("@battleSessionId", battleSessionId);

                                        try
                                        {
                                            commandUpdate.ExecuteNonQuery();
                                            Console.WriteLine("update DB success");

                                            //Delete battle from class dictionary
                                            sessionsBattle1v1AI.Remove(battleSessionId);
                                            if (!sessionsBattle1v1AI.ContainsKey(battleSessionId))
                                            {
                                                Console.WriteLine("deleted succecefully");
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("update DB FAIL");
                                        }
                                        finally
                                        {
                                            connectionToDB.Close();
                                        }

                                    }

                                });
                            }

                        }
                    }
                }
                catch
                {
                    Console.WriteLine("BattleProcess main loop error");
                }
                Thread.Sleep(50); // check dictionary every 50 ms (if 0 = 25% processor; if >1 = ~1% processor
            }
        }


        // Process the request from the client
        static private String TCPMessageProcess(String[] recievedMessage)
        {
            string answerToClient = "000";

            // Check if server is online
            if (recievedMessage[0] == "0")
            {
                Console.WriteLine("recieved online check message from client");
                answerToClient = "0";
            }

            // login
            else if (recievedMessage[0] == "1")
            {
                answerToClient = ProcessLoginRequest(recievedMessage);
            }

            // Garage manupulations
            else if (recievedMessage[0] == "2")
            {
                answerToClient = ProcessGarageRequest(recievedMessage);
            }

            // Battle 1v1 AI 
            else if (recievedMessage[0] == "3")
            {
                //Console.WriteLine("Battle 1v1 AI request");

                // from client message -> idmessage ; idplayer ; sessionToken; BattlesessionID; Code;  battle_info
                //                          [0]         [1]           [2]         [3]            [4]    [5..N]

                // player Authorization 
                if (CacheAuthorization(recievedMessage[1], recievedMessage[2]))
                {
                    // check if BattleSessionID exist and playerID is assigned to it
                    if (playerCache[recievedMessage[1]][1] == recievedMessage[3] && recievedMessage[3] != "-1")
                    {
                        // check if battle exist in class dictionary
                        if (sessionsBattle1v1AI.ContainsKey(Convert.ToInt32(recievedMessage[3])))
                        {
                            // require information for UI starting
                            if (recievedMessage[4] == "0")
                            {
                                System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
                                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                                System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

                                int battleSessionId = Convert.ToInt32(recievedMessage[3]);

                                // request to get idInArray
                                int idInArray = sessionsBattle1v1AI[battleSessionId].RequestForIdInArray(Convert.ToInt32(recievedMessage[1]));
                                Console.WriteLine("idInArray ==" + idInArray);
                                if (idInArray != -1)
                                {
                                    // new system [system]|[environment]|[playerInfo]|[OtherPlayer1]|[OtherPlayer2]...
                                    answerToClient = sessionsBattle1v1AI[battleSessionId].RequestForStartPlayerInformation(idInArray);
                                }
                                else
                                {
                                    answerToClient = "000";
                                }
                                Console.WriteLine("DEBUG - answer to client when preparing to battle = " + answerToClient);
                            }
                            // Telling server that client is ready and may start battleLoop
                            else if (recievedMessage[4] == "1")
                            {
                                int battleSessionId = Convert.ToInt32(recievedMessage[3]);
                                int idInArray = sessionsBattle1v1AI[battleSessionId].RequestForIdInArray(Convert.ToInt32(recievedMessage[1]));

                                sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].players[idInArray].playerReady = 1;
                                // answer to client
                                answerToClient = "answer for 1 - battle loop is started";
                            }
                            // require information for update UI without action
                            else if (recievedMessage[4] == "2")
                            {
                                System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
                                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                                System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

                                int battleSessionId = Convert.ToInt32(recievedMessage[3]);

                                // request to get idInArray
                                int idInArray = sessionsBattle1v1AI[battleSessionId].RequestForIdInArray(Convert.ToInt32(recievedMessage[1]));
                               // Console.WriteLine("idInArray ==" + idInArray);
                                if (idInArray != -1)
                                {
                                    // new system [system]|[environment]|[playerInfo]|[OtherPlayer1]|[OtherPlayer2]...
                                    answerToClient = sessionsBattle1v1AI[battleSessionId].RequestForUpdatePlayerInformation(idInArray);
                                }
                                else
                                {
                                    answerToClient = "000";
                                }
                               // Console.WriteLine("DEBUG - answer to client when updating to battle = " + answerToClient);









                                // ???????????????// correct it to all active weapons! 
                                //int playerWeapon1ReloadCurrent = sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotCurrentReloadTime[0];

                                //answerToClient = sessionsBattle1v1AI[battleSessionId].battleTime

                                //          + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerPositionX
                                //               + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerPositionY
                                //               + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerPositionRotation

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerFocus


                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerShipCurrentHealth
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerShipFreeEnergy

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[0]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[0]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[1]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[1]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[2]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[2]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotAdditionalInfoToClient[2]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[3]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[3]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotAdditionalInfoToClient[3]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[4]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[4]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotAdditionalInfoToClient[4]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[5]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[5]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotAdditionalInfoToClient[5]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[6]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[6]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotAdditionalInfoToClient[6]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[7]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[7]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[8]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[8]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[9]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[9]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[10]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[10]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[11]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[11]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[12]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[12]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[13]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[13]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[14]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[14]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[15]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[15]
                                //        + "," + "-1"
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotPowered[16]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSlotHealth[16]
                                //        + "," + "-1"

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotPowered[0]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotCurrentReloadTime[0]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotProjectileTime[0]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotPowered[1]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotCurrentReloadTime[1]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotProjectileTime[1]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotPowered[2]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotCurrentReloadTime[2]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotProjectileTime[2]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotPowered[3]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotCurrentReloadTime[3]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotProjectileTime[3]
                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotPowered[4]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotCurrentReloadTime[4]
                                //        + "," + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerWeaponSlotProjectileTime[4]

                                //   // ai


                                //   + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArrayEnemy].playerPositionX
                                //         + "," + sessionsBattle1v1AI[battleSessionId].players[idInArrayEnemy].playerPositionY
                                //         + "," + sessionsBattle1v1AI[battleSessionId].players[idInArrayEnemy].playerPositionRotation

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArrayEnemy].playerShipCurrentHealth

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArrayEnemy].playerWeaponSlotProjectileTime1[0, 0]

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArray].playerSumShieldCurrentCapacity

                                //    + ";" + sessionsBattle1v1AI[battleSessionId].players[idInArrayEnemy].playerSumShieldCurrentCapacity;
                            
                            
                            
                            
                            
                            
                            
                            
                            
                            
                            }
                            // require information for update UI WITH action
                            else if (recievedMessage[4] == "3")
                            {
                                int playerIdInArray = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].RequestForIdInArray(Convert.ToInt32(recievedMessage[1]));

                                // code system of pressed buttons 
                                // ????????????????????
                                if (recievedMessage[5] == "0")
                                {
                                    // TEST ONE - pressed only button attack weapon1
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerAttackModule(Convert.ToInt32(recievedMessage[6]), Convert.ToInt32(recievedMessage[7]), playerIdInArray);


                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }

                                // energy module UP
                                else if (recievedMessage[5] == "1")
                                {
                                     // up energy on the moduleSlotId
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerModuleEnergyUp(Convert.ToInt32(recievedMessage[6]), playerIdInArray);

                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                                // energy module Down
                                else if (recievedMessage[5] == "2")
                                {
                                     // down energy on the moduleSlotId
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerModuleEnergyDown(Convert.ToInt32(recievedMessage[6]), playerIdInArray);

                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                                
                                // Attack module with weapon
                                else if (recievedMessage[5] == "3")
                                {
                                    if (sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerAttackModule(Convert.ToInt32(recievedMessage[6]), Convert.ToInt32(recievedMessage[7]), playerIdInArray) == true)
                                    {
                                        answerToClient = "1"; // shoot was done
                                    }
                                    else
                                    {
                                        // answer to client  - 0 means that action was successeful
                                        answerToClient = "0";
                                    }
                                }

                                // energy weapon UP
                                else if (recievedMessage[5] == "4")
                                {
                                    // up energy on the weaponSlotId
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerWeaponEnergyUp(Convert.ToInt32(recievedMessage[6]), playerIdInArray);

                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                                // energy weapon Down
                                else if (recievedMessage[5] == "5")
                                {
                                    // down energy on the weaponSlotId
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerWeaponEnergyDown(Convert.ToInt32(recievedMessage[6]), playerIdInArray);

                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }

                                // request for move
                                else if (recievedMessage[5] == "6")
                                {
                                    // down energy on the weaponSlotId
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerSetDestinationPointToMove(recievedMessage[6], playerIdInArray);

                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                               
                                // request for focus target
                                else if (recievedMessage[5] == "7")
                                {
                                    // down energy on the weaponSlotId
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerSetFocusTarget(playerIdInArray, Convert.ToInt32(recievedMessage[6]));

                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                            }

                        }
                        else
                        {
                            // NEED TO FIX!!!! 
                            // ???? when battle is ended = 111
                            answerToClient = "111";

                            Console.WriteLine("Battle 1v1 AI request - player's battle id is not exist in class dictionary");
                        }



                    }
                    else
                    {
                        Console.WriteLine("Battle 1v1 AI request - player's battle session token is wrong");
                    }
                }
            }

            // Error response
            else
            {
                Console.WriteLine("error package from client");
                answerToClient = "000";
            }

            return answerToClient;
        }


        // Process answer from client

        // Login 
        static private string ProcessLoginRequest(String[] recievedMessage) {

            string login = recievedMessage[1];
            string password = recievedMessage[2];
            string hashedPasswordFromDB = "";
            string playerId = "";
            string answerToClient = "";

            Console.WriteLine("Recieved login require from " + login);

            string queryString = "SELECT Password  FROM Account where Login = @login ";
            string[,] queryParameters = new string[,] { { "login", login } };
            string[] stringType = new string[] { "string" };

            hashedPasswordFromDB = RequestToGetValueFromDB(queryString, stringType, queryParameters)[0][0];

            if (hashedPasswordFromDB == null)
            {
                Console.WriteLine("Player with that Login - " + login + " does not exist!");
                answerToClient = "000";
            }
            else
            {
                using var connectionToDB = new SQLiteConnection(connectionToDBString);
                connectionToDB.Open();

                //

                string hashedPasswordFromPlayer = PasswordHash(password);  // getting hashed password

                // Update player information - add session token and set start ship slot == 0
                if (hashedPasswordFromPlayer == hashedPasswordFromDB)
                {
                    var rand = new Random();
                    int randomNumber = rand.Next(000000, 999999);

                    string sessionToken = login + Convert.ToString(randomNumber);
                    //Console.WriteLine("session token = " + sessionToken);

                    // add information to the DB
                    // string enqueryUpdate = "UPDATE Account SET SessionToken = '" + sessionToken + "', GarageActiveSlot = 0  WHERE Login = '" + login + "'";
                    string enqueryUpdate = "UPDATE Account SET SessionToken = '" + sessionToken + "'  WHERE Login = '" + login + "'";

                    using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

                    try
                    {
                        commandUpdate.ExecuteNonQuery();

                        // get Player ID
                        queryString = "SELECT AccountId  FROM Account where Login = @login AND SessionToken = @sessionToken";
                        queryParameters = new string[,] { { "login", login }, { "sessionToken", sessionToken } };
                        stringType = new string[] { "int" };

                        playerId = RequestToGetValueFromDB(queryString, stringType, queryParameters)[0][0];

                        // check if cache for player is exist or not. if exist - update session token, if not exist - create new cache information
                        if (playerCache.ContainsKey(playerId))
                        {
                            playerCache[playerId][0] = sessionToken;
                        }
                        else
                        {
                            // crete cache for keeping login information for all connections after login 
                            List<string> cacheLoginList = new List<string>();
                            cacheLoginList.Add(sessionToken); // session token
                            cacheLoginList.Add("-1");          // battle session

                            playerCache.Add(playerId, cacheLoginList);
                        }
                        Console.WriteLine("Player - " + login + " login SUCCESSEFUL");

                        answerToClient = playerId + ";" + sessionToken;
                    }
                    catch
                    {
                        Console.WriteLine("ERROR updating login table with session token and creation cache information");
                    }
                }
                else
                {
                    Console.WriteLine("login UNSUCCESSEFUL (pass incorrect)");
                    answerToClient = "001";
                }

                connectionToDB.Close();
            }

            //     Console.WriteLine("!debug! answer to the clien" + answerToClient);
            return answerToClient;
        }

        // Garage
        static private string ProcessGarageRequest(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];
            string sessionToken = recievedMessage[2];


            if (CacheAuthorization(playerId, sessionToken))
            {
                answerToClient = ProcessGarageRequestAfterAuthorization(recievedMessage);
            }

            Console.WriteLine("Garage request for cache authorization from - " + playerId + " with session token - " + sessionToken + " . answer - " + answerToClient);

            return answerToClient;
        }

        static private string ProcessGarageRequestAfterAuthorization(String[] recievedMessage) {

            string answerToClient = "";

            string playerId = recievedMessage[1];
            string codeActivity = recievedMessage[3];


            if (codeActivity == "0")
            {
                if (recievedMessage[4] == "0")
                {
                    answerToClient = ReceiveGarageMainInformation(recievedMessage);
                    // Console.WriteLine("DEBUG - 1 - " + recievedMessage[4]);
                }
                else if (recievedMessage[4] == "1" || recievedMessage[4] == "2")
                {
                    answerToClient = RecieveNewShipScrollInformation(recievedMessage);
                    // Console.WriteLine("DEBUG - 2 - " + recievedMessage[4]);
                }
                else
                {
                    answerToClient = "0"; // ERROR
                }

            }
            else if (codeActivity == "1")
            {
                Console.WriteLine("Battle 1v1 AI");

                string sessionID = StartSession1v1AI(Convert.ToInt32(playerId));

                // add information about the sessionBattleID to Server Battle cache
                if (playerCache.ContainsKey(playerId))
                {
                    playerCache[playerId][1] = sessionID;
                }
                answerToClient = sessionID;
            }
            else if (codeActivity == "2")
            {
                answerToClient = RecieveGarageInventory(recievedMessage);
            }
            else if (codeActivity == "3")
            {
                answerToClient = RecieveGarageShopInformation(recievedMessage);
            }
            else if (codeActivity == "4")
            {
                answerToClient = BuyItemFromTheShop(recievedMessage);
            }
            else if (codeActivity == "5")
            {
                answerToClient = SellItemFromTheInventory(recievedMessage);
            }
            else if (codeActivity == "6")
            {
                answerToClient = RemoveItemFromTheShip(recievedMessage);
            }
            return answerToClient;
        }

        // code activite = 0 from ProcessGarageRequestAfterAuthorization
        // return AccountItemId (item connected to account) and ItemId (item Id)
        static private string ReceiveGarageMainInformation(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];

            // modules for answer start information
            // id of the item in the account-item system
            //  -1 = does not exist, 0 - empty , n - something - in account
            int accountEngineSlot = -1;
            int accountCockpitSlot = -1;
            int[] accountBigSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountMediumSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountSmallSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountWeapon = new int[] { -1, -1, -1, -1, -1 };

            // ID of the slot in item system

            int engineSlotId = 0;
            int cockpitSlotId = 0;
            int[] bigSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] mediumSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] smallSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] weaponId = new int[] { 0, 0, 0, 0, 0 };

            string[] crew = new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            int[] slotIdInfo = new int[] { 0, 0, 0 }; // middle, right, left
            int[] slotShip = new int[] { 0, 0, 0 }; // middle, right, left


            //-------------------------------------------------------------
            // SLOTS

            // AMOUNTS OF SLOTS
            string queryString = @"SELECT Garage.slot 
                            FROM Garage
                            WHERE Garage.AccountId = @playerID ";
            string[,] queryParameters = new string[,] { { "playerId", playerId } };
            string[] stringType = new string[] { "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            Console.WriteLine("amount of slots - " + requestAnswer[0].Count);
            int amountsOfSlots = requestAnswer[0].Count;


            // Active SLOT
            queryString = @"SELECT Account.GarageActiveSlot 
                            FROM Account
                            WHERE Account.AccountId = @playerID ";
            queryParameters = new string[,] { { "playerId", playerId } };
            stringType = new string[] { "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            Console.WriteLine("active garage slot - " + requestAnswer[0][0]);
            int activeSlot = Convert.ToInt32(requestAnswer[0][0]);


            //   MIDDLE SLOT

            // does not looks like a good query, because of doubling some positions (TO FIX)
            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip, Account 
                            WHERE Account.AccountId = @playerID AND Account.AccountId = Garage.AccountId
                            AND Garage.AccountShipId = AccountShip.AccountShipId AND Garage.Slot = @activeSlot
                            ";

            queryParameters = new string[,] { { "playerId", playerId }, { "activeSlot", Convert.ToString(activeSlot) } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            List<string> slots = requestAnswer[0];
            List<string> shipsID = requestAnswer[1];
            List<string> accountShipId = requestAnswer[2];

            // Console.WriteLine("DEBUG - " + requestAnswer[0].Count);
            if (shipsID.Count > 0)
            {
                slotIdInfo[0] = Convert.ToInt32(shipsID[0]);
                slotShip[0] = activeSlot;
                Console.WriteLine("DEBUG - middle slot -  " + slotShip[0]);


                //recieve information about slots 
                //-------------------------------------------------------

                queryString = @"SELECT AccountShip.AccountShipId, AccountShip.EngineSlot, AccountShip.CockpitSlot, AccountShip.BigSlot1, AccountShip.BigSlot2,
                                     AccountShip.BigSlot3, AccountShip.BigSlot4, AccountShip.BigSlot5, AccountShip.MediumSlot1, AccountShip.MediumSlot2,
                                     AccountShip.MediumSlot3, AccountShip.MediumSlot4, AccountShip.MediumSlot5, AccountShip.SmallSlot1, AccountShip.SmallSlot2,
                                     AccountShip.SmallSlot3, AccountShip.SmallSlot4, AccountShip.SmallSlot5,
                                     AccountShip.Weapon1, AccountShip.Weapon2, AccountShip.Weapon3, AccountShip.Weapon4, AccountShip.Weapon5
                            FROM AccountShip WHERE AccountShip.AccountShipId = @accountShipId";
                queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                accountEngineSlot = Convert.ToInt32(requestAnswer[1][0]);
                accountCockpitSlot = Convert.ToInt32(requestAnswer[2][0]);
                accountBigSlot[0] = Convert.ToInt32(requestAnswer[3][0]);
                accountBigSlot[1] = Convert.ToInt32(requestAnswer[4][0]);
                accountBigSlot[2] = Convert.ToInt32(requestAnswer[5][0]);
                accountBigSlot[3] = Convert.ToInt32(requestAnswer[6][0]);
                accountBigSlot[4] = Convert.ToInt32(requestAnswer[7][0]);
                accountMediumSlot[0] = Convert.ToInt32(requestAnswer[8][0]);
                accountMediumSlot[1] = Convert.ToInt32(requestAnswer[9][0]);
                accountMediumSlot[2] = Convert.ToInt32(requestAnswer[10][0]);
                accountMediumSlot[3] = Convert.ToInt32(requestAnswer[11][0]);
                accountMediumSlot[4] = Convert.ToInt32(requestAnswer[12][0]);
                accountSmallSlot[0] = Convert.ToInt32(requestAnswer[13][0]);
                accountSmallSlot[1] = Convert.ToInt32(requestAnswer[14][0]);
                accountSmallSlot[2] = Convert.ToInt32(requestAnswer[15][0]);
                accountSmallSlot[3] = Convert.ToInt32(requestAnswer[16][0]);
                accountSmallSlot[4] = Convert.ToInt32(requestAnswer[17][0]);
                accountWeapon[0] = Convert.ToInt32(requestAnswer[18][0]);
                accountWeapon[1] = Convert.ToInt32(requestAnswer[19][0]);
                accountWeapon[2] = Convert.ToInt32(requestAnswer[20][0]);
                accountWeapon[3] = Convert.ToInt32(requestAnswer[21][0]);
                accountWeapon[4] = Convert.ToInt32(requestAnswer[22][0]);



                // check system if some module installed - what id of the module 

                // DBChange11092020
                if (accountEngineSlot > 0)
                {
                    queryString = @"SELECT Engine.EngineId
                                FROM AccountShip, AccountItem, Engine, Item
                                WHERE AccountShip.AccountShipId = @accountShipId 
                                and AccountShip.EngineSlot = AccountItem.AccountItemId 
                                and AccountItem.ItemId = Item.ItemId
                                and Item.EngineId = Engine.EngineId";
                    queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                    stringType = new string[] { "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                    engineSlotId = Convert.ToInt32(requestAnswer[0][0]);
                }

                if (accountCockpitSlot > 0)
                {            // DBChange11092020
                    queryString = @"SELECT Cockpit.CockpitId
                               FROM AccountShip, AccountItem, Cockpit, Item
                               WHERE AccountShip.AccountShipId = @accountShipId
                               and AccountShip.CockpitSlot = AccountItem.AccountItemId 
                               and AccountItem.ItemId = Item.ItemId
                               and Item.CockpitId = Cockpit.CockpitId";
                    queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                    stringType = new string[] { "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                    cockpitSlotId = Convert.ToInt32(requestAnswer[0][0]);
                }

                for (int i = 0; i < accountBigSlot.Length; i++)
                {
                    if (accountBigSlot[i] > 0)
                    {// DBChange11092020
                        queryString = @"SELECT BigSlot.BigSlotId
                               FROM AccountShip, AccountItem, BigSlot, Item
                               WHERE AccountShip.AccountShipId = @accountShipId
                               and AccountShip.BigSlot" + (i + 1) + @" = AccountItem.AccountItemId 
                               and AccountItem.ItemId = Item.ItemId
                               and Item.BigSlotId = BigSlot.BigSlotId";
                        queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                        stringType = new string[] { "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                        bigSlotId[i] = Convert.ToInt32(requestAnswer[0][0]);
                    }
                }

                for (int i = 0; i < accountMediumSlot.Length; i++)
                {
                    if (accountMediumSlot[i] > 0)
                    {
                    }
                }

                for (int i = 0; i < accountSmallSlot.Length; i++)
                {
                    if (accountSmallSlot[i] > 0)
                    {
                    }
                }

                for (int i = 0; i < accountWeapon.Length; i++)
                {
                    if (accountWeapon[i] > 0)
                    {// DBChange11092020
                        int weaponNumber = i + 1;
                        queryString = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon, Item
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon" + weaponNumber + @" = AccountItem.AccountItemId
                                    and AccountItem.ItemId = Item.ItemId
                                    and Item.WeaponId = Weapon.WeaponId";
                        queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                        stringType = new string[] { "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                        weaponId[i] = Convert.ToInt32(requestAnswer[0][0]);
                    }
                }

                // ----------



                // CREWinformation
                queryString = @"SELECT AccountCrew.CrewId
                            FROM AccountCrew
                            WHERE AccountCrew.AccountId = @playerID
                            AND  AccountCrew.AccountShipId = @accountShipId";
                queryParameters = new string[,] { { "playerId", playerId }, { "accountShipId", accountShipId[0] } };
                stringType = new string[] { "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                for (int i = 0; i < requestAnswer[0].Count; i++)
                {
                    crew[i] = requestAnswer[0][i];
                }


                //-------------
            }
            else
            {
                Console.WriteLine("DEBUG - NO middle slot -  ");
                slotIdInfo[0] = 0;
                slotShip[0] = activeSlot;
            }
            // Console.WriteLine("DEBUG slot  - " + slotShip[0]);

            // int activeSlot = Convert.ToInt32(slotShip[0]);





            ////// RIGHT SLOT
            int rightSlotNumber = 0;

            if (activeSlot != (amountsOfSlots - 1))
            {
                rightSlotNumber = activeSlot + 1;
            }
            else
            {
                rightSlotNumber = 0;
            }

            // does not looks like a good query, because of doubling some positions (TO FIX)
            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                              FROM Garage, AccountShip 
                              WHERE Garage.AccountId = @playerID
                              AND (Garage.AccountShipId = AccountShip.AccountShipId AND Garage.Slot = @rightSlotNumber)
                              ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId }, { "rightSlotNumber", Convert.ToString(rightSlotNumber) } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            slots = requestAnswer[0];
            shipsID = requestAnswer[1];

            // Console.WriteLine("DEBUG - " + requestAnswer[0].Count);
            if (shipsID.Count > 0)
            {
                slotIdInfo[1] = Convert.ToInt32(shipsID[0]);
                slotShip[1] = rightSlotNumber;
            }
            else // if there is no ship in that slot
            {
                slotIdInfo[1] = 0;
                slotShip[1] = rightSlotNumber;
            }
            Console.WriteLine("DEBUG slot right - " + slotShip[1]);

            ////// LEFT SLOT
            ///
            int leftSlotNumber = 0;

            if (activeSlot != 0)
            {
                leftSlotNumber = activeSlot - 1;
                Console.WriteLine("DEBUG11 slot  left - " + leftSlotNumber);
            }
            else
            {
                leftSlotNumber = amountsOfSlots - 1;
                Console.WriteLine("DEBUG111 slot  left - " + leftSlotNumber);
            }
            // does not looks like a good query, because of doubling some positions (TO FIX)
            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                              FROM Garage, AccountShip 
                              WHERE Garage.AccountId = @playerID
                              AND (Garage.AccountShipId = AccountShip.AccountShipId AND Garage.Slot = @leftSlotNumber)
                              ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId }, { "leftSlotNumber", Convert.ToString(leftSlotNumber) } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            slots = requestAnswer[0];
            shipsID = requestAnswer[1];

            //  Console.WriteLine("DEBUG - " + requestAnswer[1].Count);
            if (shipsID.Count > 0)
            {
                Console.WriteLine("DEBUG slot  left exist - ship essigned ");
                slotIdInfo[2] = Convert.ToInt32(shipsID[0]);
                slotShip[2] = leftSlotNumber;
            }
            else // if there is no ship in that slot
            {
                Console.WriteLine("DEBUG slot  left no ship assignet - ");
                slotIdInfo[2] = 0;
                slotShip[2] = leftSlotNumber;
            }
            Console.WriteLine("DEBUG slot  left - " + slotShip[2]);


            //-------------------------------------------------------------



            Console.WriteLine("l - " + leftSlotNumber + " m -" + activeSlot + " r - " + rightSlotNumber);


            // Answer should include AccountItemId (for future manupulations with item) and ItemId 
            //(for big slot - does not matter if shield of etc, because DB system duplicated in the client)
            answerToClient = slotIdInfo[0] + ";" + slotIdInfo[1] + ";" + slotIdInfo[2] +
                 ";" + accountEngineSlot + ";" + engineSlotId + ";" + accountCockpitSlot + ";" + cockpitSlotId +
                 ";" + accountBigSlot[0] + ";" + bigSlotId[0] + ";" + accountBigSlot[1] + ";" + bigSlotId[1] +
                 ";" + accountBigSlot[2] + ";" + bigSlotId[2] + ";" + accountBigSlot[3] + ";" + bigSlotId[3] +
                 ";" + accountBigSlot[4] + ";" + bigSlotId[4] +
                 ";" + accountMediumSlot[0] + ";" + mediumSlotId[0] + ";" + accountMediumSlot[1] + ";" + mediumSlotId[1] +
                 ";" + accountMediumSlot[2] + ";" + mediumSlotId[2] + ";" + accountMediumSlot[3] + ";" + mediumSlotId[3] +
                 ";" + accountMediumSlot[4] + ";" + mediumSlotId[4] +
                 ";" + accountSmallSlot[0] + ";" + smallSlotId[0] + ";" + accountSmallSlot[1] + ";" + smallSlotId[1] +
                 ";" + accountSmallSlot[2] + ";" + smallSlotId[2] + ";" + accountSmallSlot[3] + ";" + smallSlotId[3] +
                 ";" + accountSmallSlot[4] + ";" + smallSlotId[4] +
                 ";" + accountWeapon[0] + ";" + weaponId[0] + ";" + accountWeapon[1] + ";" + weaponId[1] +
                 ";" + accountWeapon[2] + ";" + weaponId[2] + ";" + accountWeapon[3] + ";" + weaponId[3] +
                 ";" + accountWeapon[4] + ";" + weaponId[4] +
                 ";" + slotShip[0] + ";" + slotShip[1] + ";" + slotShip[2] +
                 ";" + crew[0] + ";" + crew[1] + ";" + crew[2] + ";" + crew[3] + ";" + crew[4] + ";" + crew[5] +
                 ";" + crew[6] + ";" + crew[7] + ";" + crew[8] + ";" + crew[9] + ";" + crew[10] + ";" + crew[11] +
                 ";" + crew[12] + ";" + crew[13] + ";" + crew[14];

            return answerToClient;
        }

        static private string RecieveNewShipScrollInformation(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];
            string slotShipId = recievedMessage[4];  // 1 - left, 2 - right

            int newSlotNumber = 0;

            // AMOUNTS OF SLOTS
            string queryString = @"SELECT Garage.slot 
                            FROM Garage
                            WHERE Garage.AccountId = @playerID ";
            string[,] queryParameters = new string[,] { { "playerId", playerId } };
            string[] stringType = new string[] { "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            Console.WriteLine("amount of slots - " + requestAnswer[0].Count);
            int amountsOfSlots = requestAnswer[0].Count;


            // Active SLOT
            queryString = @"SELECT Account.GarageActiveSlot 
                            FROM Account
                            WHERE Account.AccountId = @playerID ";
            queryParameters = new string[,] { { "playerId", playerId } };
            stringType = new string[] { "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            Console.WriteLine("active garage slot - " + requestAnswer[0][0]);
            int activeSlot = Convert.ToInt32(requestAnswer[0][0]);

            newSlotNumber = activeSlot; // just in case that something will go wrong - to load current ship


            // get new active slot number 
            if (slotShipId == "1")
            {
                Console.WriteLine("change slot to left ");

                if (activeSlot != 0)
                {
                    newSlotNumber = activeSlot - 1;
                }
                else
                {
                    newSlotNumber = amountsOfSlots - 1;
                }
            }
            else if (slotShipId == "2")
            {
                Console.WriteLine("change slot to right ");

                if (activeSlot != (amountsOfSlots - 1))
                {
                    newSlotNumber = activeSlot + 1;
                }
                else
                {
                    newSlotNumber = 0;
                }
            }

            // set new active slot number 
            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();
            string enqueryUpdate = "UPDATE Account SET GarageActiveSlot = '" + newSlotNumber + "' WHERE AccountId = '" + recievedMessage[1] + "' AND SessionToken = '" + recievedMessage[2] + "' ";
            using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

            try
            {
                commandUpdate.ExecuteNonQuery();
            }
            catch
            {
                Console.WriteLine("ERROR updating login table with new GarageActiveSlot information");
            }
            finally
            {
                connectionToDB.Close();
            }

            // request to get information about ship and right\left slots
            answerToClient = ReceiveGarageMainInformation(recievedMessage);

            return answerToClient;
        }


        static private void GetInformationForTheGarage()
        {


        }
        // code activite = 2 from ProcessGarageRequestAfterAuthorization
        static private string RecieveGarageInventory(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];

            Console.WriteLine("Inventory information ");
            string answerType = "";
            string answerItemTypeId = "";
            string answerId = "";

            // DBChange11092020
            string queryString = @"SELECT AccountItem.AccountItemId, AccountItem.Amount,
                                Item.CockpitId, Item.EngineId, Item.WeaponId, Item.BigSlotId, Item.MediumSlotId, Item.SmallSlotId 
                                FROM AccountItem, Item 
                                WHERE AccountItem.AccountId = @playerID AND AccountItem.AccountShipId = 0 AND AccountItem.ItemId = Item.ItemId";

            // string queryString = "SELECT * FROM AccountItem WHERE AccountItem.AccountId = @playerID AND AccountItem.AccountShipId = 0";
            string[,] queryParameters = new string[,] { { "playerID", playerId } };
            string[] stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            // starts from 4 - because item type starts from 4 in DB
            for (int i = 0; i < requestAnswer[0].Count; i++)
            {
                for (int ii = 0; ii < 6; ii++)
                {
                    if (Convert.ToInt32(requestAnswer[ii + 2][i]) != 0)
                    {
                        answerId = requestAnswer[0][i];
                        answerType = "" + ii;
                        answerItemTypeId = requestAnswer[ii + 2][i];
                    }
                }
                answerToClient = answerToClient + answerId + ";" + answerType + ";" + answerItemTypeId + ";";
            }
            answerToClient = answerToClient.Remove(answerToClient.Length - 1);

            return answerToClient;
        }

        static private string RecieveGarageShopInformation(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];

            Console.WriteLine("Player id=" + playerId + " ask shop information ");

            string queryString = @"SELECT ShopItem.ShopItemId, ShopItem.Price,
                        Item.EngineId, Item.CockpitId, Item.WeaponId, Item.BigSlotId, Item.MediumSlotId, Item.SmallSlotId
                        FROM ShopItem, Item
                        WHERE ShopItem.ItemId = Item.ItemId";
            string[,] queryParameters = new string[,] { };
            string[] stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            //    0      1       2       3      4        5         6        7
            //[itemID, price, cockpit,engine,weapon,bigslot,mediumslot,smallslot]


            for (int i = 0; i < requestAnswer[0].Count; i++)
            {
                answerToClient = answerToClient + requestAnswer[0][i] + ";" + requestAnswer[1][i];

                if (requestAnswer[2][i] != "0")
                {
                    answerToClient = answerToClient + ";" + 0 + ";" + requestAnswer[2][i];
                }
                else if (requestAnswer[3][i] != "0")
                {
                    answerToClient = answerToClient + ";" + 1 + ";" + requestAnswer[3][i];
                }
                else if (requestAnswer[4][i] != "0")
                {
                    answerToClient = answerToClient + ";" + 2 + ";" + requestAnswer[4][i];
                }
                else if (requestAnswer[5][i] != "0")
                {
                    answerToClient = answerToClient + ";" + 3 + ";" + requestAnswer[5][i];
                }
                else if (requestAnswer[6][i] != "0")
                {
                    answerToClient = answerToClient + ";" + 4 + ";" + requestAnswer[6][i];
                }
                else if (requestAnswer[7][i] != "0")
                {
                    answerToClient = answerToClient + ";" + 5 + ";" + requestAnswer[7][i];
                }
                else
                {
                    answerToClient = answerToClient + ";" + 0 + ";" + 0;
                }

                if (i != (requestAnswer[0].Count - 1))
                {
                    answerToClient = answerToClient + ";";
                }
            }

            // send itemId to buy, price, typeId, typeItemId
            return answerToClient;
        }


        static private string BuyItemFromTheShop(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];

            Console.WriteLine("DEBUG BUY SHOPT ITEM - " + recievedMessage[4]);

            string queryString = @"SELECT ShopItem.Price, ShopItem.ItemId
                             FROM ShopItem
                            WHERE ShopItem.ShopItemId = @ShopItemId";
            string[,] queryParameters = new string[,] { { "ShopItemId", Convert.ToString(recievedMessage[4]) } };
            string[] stringType = new string[] { "int", "int" };
            List<string>[] requestAnswerItem = RequestToGetValueFromDB(queryString, stringType, queryParameters);


            queryString = "SELECT Money FROM Account WHERE Account.AccountId = @playerID";
            queryParameters = new string[,] { { "playerID", playerId } };
            stringType = new string[] { "int" };
            List<string>[] requestAnswerMoney = RequestToGetValueFromDB(queryString, stringType, queryParameters);


            // if enough money in account
            if (Convert.ToInt32(requestAnswerMoney[0][0]) >= Convert.ToInt32(requestAnswerItem[0][0]))
            {
                //-----------------------------

                using var connectionToDB = new SQLiteConnection(connectionToDBString);
                connectionToDB.Open();


                // Create Session with SesionID, playerID, playerSlot
                // add information to the DB
                string enqueryUpdate = @"INSERT INTO AccountItem (AccountId, Amount, AccountShipId, ItemId) 
                        VALUES (@playerID, 1, 0, @itemId)";

                using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

                commandUpdate.Parameters.AddWithValue("@playerID", playerId);

                commandUpdate.Parameters.AddWithValue("@itemId", requestAnswerItem[1][0]);


                try
                {
                    commandUpdate.ExecuteNonQuery();
                    //commandUpdate.ExecuteScalar();


                    //----

                    string enqueryUpdate1 = @"UPDATE Account
                             SET Money = @newMoney
                            WHERE AccountId = @playerID";
                    using var commandUpdate1 = new SQLiteCommand(enqueryUpdate1, connectionToDB);
                    commandUpdate1.Parameters.AddWithValue("@playerID", playerId);
                    commandUpdate1.Parameters.AddWithValue("@newMoney", Convert.ToString((Convert.ToInt32(requestAnswerMoney[0][0]) - Convert.ToInt32(requestAnswerItem[1][0]))));




                    try
                    {
                        commandUpdate1.ExecuteNonQuery();
                    }
                    catch (InvalidCastException e)
                    {
                        Console.WriteLine("error updating DB for buying an item - update money" + e);
                    }
                    //------------------

                }
                catch (InvalidCastException e)
                {
                    Console.WriteLine("error updating DB for buying an item" + e);
                }
                finally
                {
                    connectionToDB.Close();
                }
                //----------------------------------------
            }

            answerToClient = "1";

            return answerToClient;
        }

        static private string SellItemFromTheInventory(String[] recievedMessage)
        {
            string answerToClient = "";

            Console.WriteLine("DEBUG Delete ITEM from inventory - " + recievedMessage[4]);

            answerToClient = "1";

            return answerToClient;
        }

        static private string RemoveItemFromTheShip(String[] recievedMessage)
        {
            string answerToClient = "";
            string playerId = recievedMessage[1];
            string slotToSelect = "";
            string slotToUpdate = "";
            Console.WriteLine("DEBUG Delete ITEM from ship - " + recievedMessage[4]);


            if (recievedMessage[4] == "0")
            {
                slotToSelect = "AccountShip.EngineSlot";
                slotToUpdate = "EngineSlot";
            }
            else if (recievedMessage[4] == "1")
            {
                slotToSelect = "AccountShip.CockpitSlot";
                slotToUpdate = "CockpitSlot";
            }
            else if (recievedMessage[4] == "2")
            {
                slotToSelect = "AccountShip.Weapon1";
                slotToUpdate = "Weapon1";
            }
            else if (recievedMessage[4] == "3")
            {
                slotToSelect = "AccountShip.Weapon2";
                slotToUpdate = "Weapon2";
            }
            else if (recievedMessage[4] == "4")
            {
                slotToSelect = "AccountShip.Weapon3";
                slotToUpdate = "Weapon3";
            }
            else if (recievedMessage[4] == "5")
            {
                slotToSelect = "AccountShip.Weapon4";
                slotToUpdate = "Weapon4";
            }
            else if (recievedMessage[4] == "6")
            {
                slotToSelect = "AccountShip.Weapon5";
                slotToUpdate = "Weapon5";
            }
            else if (recievedMessage[4] == "7")
            {
                slotToSelect = "AccountShip.BigSlot1";
                slotToUpdate = "BigSlot1";
            }
            else if (recievedMessage[4] == "8")
            {
                slotToSelect = "AccountShip.BigSlot2";
                slotToUpdate = "BigSlot2";
            }
            else if (recievedMessage[4] == "9")
            {
                slotToSelect = "AccountShip.BigSlot3";
                slotToUpdate = "BigSlot3";
            }
            else if (recievedMessage[4] == "10")
            {
                slotToSelect = "AccountShip.BigSlot4";
                slotToUpdate = "BigSlot4";
            }
            else if (recievedMessage[4] == "11")
            {
                slotToSelect = "AccountShip.BigSlot5";
                slotToUpdate = "BigSlot5";
            }
            else if (recievedMessage[4] == "12")
            {
                slotToSelect = "AccountShip.MediumSlot1";
                slotToUpdate = "MediumSlot1";
            }
            else if (recievedMessage[4] == "13")
            {
                slotToSelect = "AccountShip.MediumSlot2";
                slotToUpdate = "MediumSlot2";
            }
            else if (recievedMessage[4] == "14")
            {
                slotToSelect = "AccountShip.MediumSlot3";
                slotToUpdate = "MediumSlot3";
            }
            else if (recievedMessage[4] == "15")
            {
                slotToSelect = "AccountShip.MediumSlot4";
                slotToUpdate = "MediumSlot4";
            }
            else if (recievedMessage[4] == "16")
            {
                slotToSelect = "AccountShip.MediumSlot5";
                slotToUpdate = "MediumSlot5";
            }
            else if (recievedMessage[4] == "17")
            {
                slotToSelect = "AccountShip.SmallSlot1";
                slotToUpdate = "SmallSlot1";
            }
            else if (recievedMessage[4] == "18")
            {
                slotToSelect = "AccountShip.SmallSlot2";
                slotToUpdate = "SmallSlot2";
            }
            else if (recievedMessage[4] == "19")
            {
                slotToSelect = "AccountShip.SmallSlot3";
                slotToUpdate = "SmallSlot3";
            }
            else if (recievedMessage[4] == "20")
            {
                slotToSelect = "AccountShip.SmallSlot4";
                slotToUpdate = "SmallSlot4";
            }
            else if (recievedMessage[4] == "21")
            {
                slotToSelect = "AccountShip.SmallSlot5";
                slotToUpdate = "SmallSlot5";
            }

            string queryString = @"SELECT " + slotToSelect + @", AccountShip.AccountShipId
                            FROM Account, Garage, AccountShip
                            WHERE Account.AccountId = @playerId and Account.GarageActiveSlot = Garage.Slot 
                            AND Garage.AccountShipId = AccountShip.AccountShipId";
            string[,] queryParameters = new string[,] { { "playerId", playerId } };

            string[] stringType = new string[] { "int", "int" };
            List<string>[] requestAnswerItem = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            Console.WriteLine("DEBUG TEST UNFIT" + requestAnswerItem[0][0]);

            if (requestAnswerItem[0][0] != "0" && requestAnswerItem[0][0] != "-1")
            {
                //remove slot from the ship
                Console.WriteLine("DEBUG TEST UNFIT 1" + requestAnswerItem[0][0]);


                // set new active slot number 
                using var connectionToDB = new SQLiteConnection(connectionToDBString);
                connectionToDB.Open();
                string enqueryUpdate = @"UPDATE AccountShip SET " + slotToUpdate + @" = 0 WHERE AccountShipId = " + requestAnswerItem[1][0] + @"";
                using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

                try
                {
                    commandUpdate.ExecuteNonQuery();

                    // change slot placement to inventory
                    enqueryUpdate = @"UPDATE AccountItem SET AccountShipId = 0 WHERE AccountItemId = " + requestAnswerItem[0][0] + @"";
                    using var commandUpdate2 = new SQLiteCommand(enqueryUpdate, connectionToDB);
                    try
                    {
                        commandUpdate2.ExecuteNonQuery();
                    }
                    catch
                    {
                        Console.WriteLine("ERROR updating login table with new GarageActiveSlot information 2");
                    }
                }
                catch
                {
                    Console.WriteLine("ERROR updating login table with new GarageActiveSlot information 1");
                }
                finally
                {
                    connectionToDB.Close();
                }









            }

            answerToClient = "1";

            return answerToClient;
        }


        //==
        // get coordintates for players to set at the start
        static private string[] GetPlayersPositionsToSetAtStart(int mapIdStart)
        {
            string queryString = "SELECT PositionsTeam1, PositionsTeam2  FROM BattleMap where BattleMapId = @mapId ";
            string[,] queryParameters = new string[,] { { "mapId", Convert.ToString(mapIdStart) } };
            string[] stringType = new string[] { "string", "string" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);


            string[] answer = new string[3];
            answer[0] = Convert.ToString(mapIdStart);
            answer[1] = requestAnswer[0][0];
            answer[2] = requestAnswer[1][0];
            return answer;
        }
        /*
         * --------------------------------------
             REUSABLE FUNCTIONS
           --------------------------------------
        */

        // TCP process information - getting and sending the message from\to client
        static public void TCPServer()
        {

            TcpListener server = null;
            try
            {
                // Set the TcpListener  
                IPAddress ip = IPAddress.Any;

                server = new TcpListener(ip, port);
                // Console.WriteLine("I am listening for connections on " +
                //                                         IPAddress.Parse(((IPEndPoint)server.LocalEndpoint).Address.ToString()) +
                //                                          "on port number " + ((IPEndPoint)server.LocalEndpoint).Port.ToString());

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[1024]; // 64 symbols
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    try
                    {
                        TcpClient client = server.AcceptTcpClient();
                        if (client.Connected)
                        {

                            Task.Run(() =>
                            {  // question is Task or Parallel -> what is really works in parallel

                                // Getting the IP adress of the client
                                var localEndPoint = client.Client.LocalEndPoint as IPEndPoint;
                                var localAddress = localEndPoint.Address;
                                var localPort = localEndPoint.Port;

                                var clientIPAddress = client.Client.RemoteEndPoint;
                                //Console.WriteLine("connected to adress - " + localAddress + "; port - " + localPort + "; Remote IP address -" + clientIPAddress);

                                // Get a stream object for reading and writing
                                NetworkStream stream = client.GetStream();

                                // Loop to receive all the data sent by the client.
                                data = null;
                                int i;
                                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                                {
                                    // Decompress DATA


                                    // Translate data bytes to a ASCII string.
                                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                    //Console.WriteLine("Received: {0}", data);

                                    // transfer message to an array with ; split
                                    String[] separator = { ";" };
                                    String[] dataSeparator = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                                    // Process the recieved data
                                    string dataToSend = TCPMessageProcess(dataSeparator);

                                    // Process the data sent by the client.
                                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(dataToSend);

                                    // COMPRESS DATA 


                                    // Send back a response.
                                    stream.Write(msg, 0, msg.Length);
                                    // Console.WriteLine("Sent: {0}", dataToSend);
                                   // Console.WriteLine("Sent size: {0}", msg.Length);
                                }

                                // Shutdown and end connection
                                client.Close();
                            });
                        }


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("TCP thread connection error: ", e);
                    }

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }


        // Request to DB to recieve value from SINGLE and MULTIPLE column (SELECT QUERY)
        static private List<string>[] RequestToGetValueFromDB(string queryString, string[] readerValueType, string[,] queryParameters)
        {
            List<string>[] queryResult = new List<string>[readerValueType.Length];

            for (int i = 0; i < readerValueType.Length; i++)
            {
                queryResult[i] = new List<string>();
            }

            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();
            using var cmd = new SQLiteCommand(queryString, connectionToDB);

            for (int i = 0; i < queryParameters.Length / 2; i++)
            {
                cmd.Parameters.AddWithValue("@" + queryParameters[i, 0], queryParameters[i, 1]);
            }
            try
            {
                using SQLiteDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < readerValueType.Length; i++)
                        {
                            //transfer all to string and then figure out what is what after returning the value after function
                            if (readerValueType[i] == "string") {
                                queryResult[i].Add(reader.GetString(i));
                            }
                            else if (readerValueType[i] == "int")
                            {
                                queryResult[i].Add(Convert.ToString(reader.GetInt32(i)));
                            }
                        }
                    }
                }
                else
                {
                    queryResult[0].Add(null); // return null value ? maybe fill all answer with null value in future????
                    Console.WriteLine("RequestToGetValueFromDB - No rows found.");
                }
                reader.Close();
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("error with recieveing information from login table RequestToGetValueFromDB" + e);
            }
            connectionToDB.Close();

            return queryResult;
        }


        // Authotorise player through cache
        static private bool CacheAuthorization(string playerId, string sessionToken)
        {
            bool answer = false;

            // Check if player exist in cache 
            if (playerCache.ContainsKey(playerId))
            {
                // Console.WriteLine("Player exist in session cache");

                // check if player legit
                if (playerCache[playerId][0] == sessionToken)
                {
                    // Console.WriteLine("Player Authorized");
                    answer = true;
                }
                else
                {
                    Console.WriteLine("Player is not Authorized");
                }
            }
            else
            {
                Console.WriteLine("Player does not exist in session cache");
            }

            return answer;
        }



        // Other functions 

        // Password hash check
        static private String PasswordHash(string password)
        {

            byte[] salt = Convert.FromBase64String("xS2EOJVGIZl5hxRDEiQO3g==");

            // generate a 128-bit salt using a secure PRNG
            //byte[] salt = new byte[128 / 8];
            //using (var rng = RandomNumberGenerator.Create())
            //{
            //    rng.GetBytes(salt);
            //}
            //Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            //  Console.WriteLine($"Hashed: {hashed}");

            return hashed;
        }





        // Initialiation of the battle session
        static private string StartSession1v1AI(int playerID)
        {
            // Check slot of the player 
            int activeSlot = 0;
            int newBattleID = -1;

            string queryString = "SELECT GarageActiveSlot  FROM Account where AccountId = @playerID ";
            string[,] queryParameters = new string[,] { { "playerID", Convert.ToString(playerID) } };
            string[] stringType = new string[] { "int" };

            activeSlot = Convert.ToInt32(RequestToGetValueFromDB(queryString, stringType, queryParameters)[0][0]);


            //-----------------------------

            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();


            // Create Session with SesionID, playerID, playerSlot
            // add information to the DB
            string enqueryUpdate = "INSERT INTO Battle1v1Ai (AccountId, AiId, Active)  VALUES (@playerID, 0, 1);SELECT last_insert_rowid()"; //  last_insert_rowid() for sqllite
            //OUTPUT INSERTED.Battle_1v1_AI_ID
            using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

            commandUpdate.Parameters.AddWithValue("@playerID", playerID);

            try
            {
                //var id_battle = (int)commandUpdate.ExecuteNonQuery();
                // commandUpdate.ExecuteNonQuery();
                newBattleID = Convert.ToInt32(commandUpdate.ExecuteScalar());
                //commandUpdate.ExecuteScalar();
                // Console.WriteLine("id_battle - " + newBattleID);
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("error updating DB for creating new session for 1v1 A - I" + e);
            }
            connectionToDB.Close();

            //----------------------------------------
            //----------------------------------------
            try
            {
                // NvN - 1 player + N AI vs N AI


                // 1v1 PlayervAI

                //int[] playersToSet = new int[] { 0, 1 };  // 0 - player, 1 - AI
                //int[] playersTeamsToSet = new int[] { 0, 1 };
                //int mapId = 1;
                //string[] mapToSet = GetPlayersPositionsToSetAtStart(mapId);

                //// Start class with battle parameters
                //sessionsBattle1v1AI.Add(newBattleID, new BattleSession(playersToSet, playersTeamsToSet, mapToSet));
                //Console.WriteLine("new session with number - " + newBattleID + " is started with player -  " + playerID);

                //// add player who create the battle
                //int idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(0, 0, playerID); // Type \ team \ PlayerIDFromDB
                //if (idInArray != -1)
                //{
                //    SessionStartLoadPlayer(Convert.ToString(playerID), newBattleID, idInArray);
                //}

                //int aIId = 1;
                //idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(1, 1, aIId); // Type \ team \ PlayerIDFromDB
                //Console.WriteLine("idInArray when created session = " + idInArray);
                //if (idInArray != -1)
                //{
                //    SessionStartLoadAI(Convert.ToString(aIId), newBattleID, idInArray);
                //}


                //==============================================
                //==============================================


                // 3v3 PlayervAI - test

                int[] playersToSet = new int[] { 0, 1, 1, 1, 1, 1 };  // 0 - player, 1 - AI
                int[] playersTeamsToSet = new int[] { 0, 0, 0, 1, 1, 1 };


                int mapId = 1;
                string[] mapToSet = GetPlayersPositionsToSetAtStart(mapId);

                // Start class with battle parameters
                sessionsBattle1v1AI.Add(newBattleID, new BattleSession(playersToSet, playersTeamsToSet, mapToSet));
                Console.WriteLine("new session with number - " + newBattleID + " is started with player -  " + playerID);


                    // add player who create the battle
                    int idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(0, 0, playerID); // Type \ team \ PlayerIDFromDB
                    if (idInArray != -1)
                    {
                        SessionStartLoadPlayer(Convert.ToString(playerID), newBattleID, idInArray);
                    }

                    int aIId = 1;
                    idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(1, 0, aIId); // Type \ team \ PlayerIDFromDB
                    Console.WriteLine("idInArray when created session = " + idInArray);
                    if (idInArray != -1)
                    {
                        SessionStartLoadAI(Convert.ToString(aIId), newBattleID, idInArray);
                    }

                    idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(1, 0, aIId); // Type \ team \ PlayerIDFromDB
                    Console.WriteLine("idInArray when created session = " + idInArray);
                    if (idInArray != -1)
                    {
                        SessionStartLoadAI(Convert.ToString(aIId), newBattleID, idInArray);
                    }

                    idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(1, 1, aIId); // Type \ team \ PlayerIDFromDB
                    Console.WriteLine("idInArray when created session = " + idInArray);
                    if (idInArray != -1)
                    {
                        SessionStartLoadAI(Convert.ToString(aIId), newBattleID, idInArray);
                    }

                    idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(1, 1, aIId); // Type \ team \ PlayerIDFromDB
                    Console.WriteLine("idInArray when created session = " + idInArray);
                    if (idInArray != -1)
                    {
                        SessionStartLoadAI(Convert.ToString(aIId), newBattleID, idInArray);
                    }

                    idInArray = sessionsBattle1v1AI[newBattleID].AddPlayer(1, 1, aIId); // Type \ team \ PlayerIDFromDB
                    Console.WriteLine("idInArray when created session = " + idInArray);
                    if (idInArray != -1)
                    {
                        SessionStartLoadAI(Convert.ToString(aIId), newBattleID, idInArray);
                    }
   
              


                //==============================================
                //==============================================
                //==============================================

                sessionsBattle1v1AI[newBattleID].toStart = 1;
                Console.WriteLine("SESSION TO START - YES - " + sessionsBattle1v1AI[newBattleID].toStart);
            }
            catch
            {
                Console.WriteLine("Unable to create and fill up battle session");
            }

            //  -----------------------------------------------------


            return Convert.ToString(newBattleID);
        }




        // get information about the modules \ ships \ crew \ weapons for battle tp the class

            // corrected to new class (maybe connect somehow to player
        static private void SessionStartLoadAI(string aiId, int newBattleID, int idInArray)
        {
            Console.WriteLine("DEBUG Session1v1AILoadAI - TRYING TO LOAD AI  ");
            // get information about ID of all modules and ship
            string queryString = @"SELECT AiShip.AiShipId, AiShip.ShipId, AiShip.EngineSlot, AiShip.CockpitSlot, AiShip.BigSlot1, AiShip.BigSlot2,
                                     AiShip.BigSlot3, AiShip.BigSlot4, AiShip.BigSlot5, AiShip.MediumSlot1, AiShip.MediumSlot2,
                                     AiShip.MediumSlot3, AiShip.MediumSlot4, AiShip.MediumSlot5, AiShip.SmallSlot1, AiShip.SmallSlot2,
                                     AiShip.SmallSlot3, AiShip.SmallSlot4, AiShip.SmallSlot5,
                                     AiShip.Weapon1, AiShip.Weapon2, AiShip.Weapon3, AiShip.Weapon4, AiShip.Weapon5
                            FROM Ai, AiShip
							WHERE 
							Ai.AiId = @aiId
                            and Ai.AiShipId = AiShip.AiShipId";
            string[,] queryParameters = new string[,] { { "aiId", aiId } };
            string[] stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            string aiShipId = requestAnswer[0][0];
            string shipId = requestAnswer[1][0];
            string engineSlotId = requestAnswer[2][0];
            string cockpitSlotId = requestAnswer[3][0];
            string[] bigSlotId = new string[] { requestAnswer[4][0], requestAnswer[5][0], requestAnswer[6][0], requestAnswer[7][0], requestAnswer[8][0] };
            string[] mediumSlotId = new string[] { requestAnswer[9][0], requestAnswer[10][0], requestAnswer[11][0], requestAnswer[12][0], requestAnswer[13][0] };
            string[] smallSlotId = new string[] { requestAnswer[14][0], requestAnswer[15][0], requestAnswer[16][0], requestAnswer[17][0], requestAnswer[18][0] };

            string[] weaponSlotId = new string[] { requestAnswer[19][0], requestAnswer[20][0], requestAnswer[21][0], requestAnswer[22][0], requestAnswer[23][0] };


          //  Console.WriteLine("DEBUG Session1v1AILoadAI - 2  ");
            // get information about the ship info
            queryString = @"SELECT Ship.BaseHealth, Ship.BaseEnergy
                             FROM Ship
                             WHERE Ship.ShipId = @shipId";
            queryParameters = new string[,] { { "shipId", shipId } };
            stringType = new string[] { "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipId = Convert.ToInt32(shipId);
            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipMaxHealth = Convert.ToInt32(requestAnswer[0][0]);
            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipMaxEnergy = Convert.ToInt32(requestAnswer[1][0]);
            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipFreeEnergy = Convert.ToInt32(requestAnswer[1][0]);


           // Console.WriteLine("DEBUG Session1v1AILoadAI - 3  ");
            // get information about the engine slot
            if (engineSlotId != "-1" && engineSlotId != "0")
            {
                queryString = @"SELECT Engine.Health, Engine.Energy, Engine.EngineId
                            FROM Engine
                             WHERE Engine.EngineId = @engineId";
                queryParameters = new string[,] { { "engineId", engineSlotId } };
                stringType = new string[] { "int", "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[0] = Convert.ToInt32(requestAnswer[2][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[0] = 0;
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[0] = "engine";
            }
            else if(engineSlotId == "0")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[0] = 0;
            }
            else if (engineSlotId == "-1")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[0] = -1;
            }

          //  Console.WriteLine("DEBUG Session1v1AILoadAI - 4  " + cockpitSlotId);
            // get information about the cockpit slot
            if (cockpitSlotId != "-1" && cockpitSlotId != "0")
            {
                queryString = @"SELECT Cockpit.Health, Cockpit.Energy,Cockpit.CockpitId 
                            FROM Cockpit
                             WHERE Cockpit.CockpitId = @cockpitId";
                queryParameters = new string[,] { { "cockpitId", cockpitSlotId } };
                stringType = new string[] { "int", "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[1] = Convert.ToInt32(requestAnswer[2][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[1] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[1] = 0;
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[1] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[1] = "cockpit";
            }
            else if(cockpitSlotId == "0")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[1] = 0;
            }
            else if (cockpitSlotId == "-1")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[1] = -1;
            }

          //  Console.WriteLine("DEBUG Session1v1AILoadAI - 5  ");
            // get information about the bigslots
            for (int i = 0; i < bigSlotId.Length; i++)
            {
                if (bigSlotId[i] != "-1" && bigSlotId[i] != "0")
                {
                 //   Console.WriteLine("DEBUG Session1v1AILoadAI - 5 - 1 ");

                    queryString = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId, BigSlot.BigSlotId
                                FROM BigSlot
                                 WHERE BigSlot.BigSlotId = @bigSlotId";
                    queryParameters = new string[,] { { "bigSlotId", bigSlotId[i] } };
                    stringType = new string[] { "int", "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 2] = Convert.ToInt32(requestAnswer[2][0]);

                 //   Console.WriteLine("DEBUG Session1v1AILoadAI - 5 - 11  ");
                    //get information if shield or weaponcontrol
                    if (requestAnswer[0][0] != "0") //shield
                    {
                        queryString = @"SELECT Shield.Heath, Shield.Energy, Shield.Capacity, Shield.RechargeTime, Shield.RechargeRate
                                        FROM Shield
                                        WHERE Shield.ShieldId = @shieldId";
                        queryParameters = new string[,] { { "shieldId", requestAnswer[0][0] } };
                        stringType = new string[] { "int", "int", "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[i + 2] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[i + 2] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[i + 2] = "shield";

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldCapacity[i + 2] = Convert.ToInt32(requestAnswer[2][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldRechargeTime[i + 2] = Convert.ToInt32(requestAnswer[3][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldRechargeCurrentTime[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldRechargeRate[i + 2] = Convert.ToInt32(requestAnswer[4][0]);


                    }
                    else if (requestAnswer[1][0] != "0") // weapon control
                    {
                        queryString = @"SELECT WeaponContol.Health, WeaponContol.Energy, WeaponContol.AmountOfWeapons
                        FROM WeaponContol
                        WHERE WeaponContol.WeaponControlId = @weaponControlId";
                        queryParameters = new string[,] { { "weaponControlId", requestAnswer[1][0] } };
                        stringType = new string[] { "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[0] = 0;
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[0] = "weaponcontrol";
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotWeaponControlAmountOfWeapons[0] = Convert.ToInt32(requestAnswer[2][0]);
                    }
                }
                else if(bigSlotId[i] == "0")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 2] = 0;
                }
                else if (bigSlotId[i] == "-1")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 2] = -1;
                }

              //  Console.WriteLine("DEBUG Session1v1AILoadAI - 5 - 2  ");
            }

         //   Console.WriteLine("DEBUG Session1v1AILoadAI - 6  ");
            // get information about the middleslots
            for (int i = 0; i < mediumSlotId.Length; i++)
            {
                if (mediumSlotId[i] != "-1" && mediumSlotId[i] != "0")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 7] = 1;
                }
                else if(mediumSlotId[i] == "0")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 7] = 0;
                }
                else if (mediumSlotId[i] == "-1")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 7] = -1;
                }
            }

           // Console.WriteLine("DEBUG Session1v1AILoadAI - 7  ");
            // get information about the weapons
            for (int i = 0; i < weaponSlotId.Length; i++)
            {
                if (weaponSlotId[i] != "-1" && weaponSlotId[i] != "0")
                {


                    queryString = @"SELECT Weapon.Energy, Weapon.Damage, Weapon.ReloadTime, Weapon.WeaponId
                                    FROM Weapon
                                    WHERE Weapon.WeaponId = @weaponId";
                    queryParameters = new string[,] { { "weaponId", weaponSlotId[i] } };
                    stringType = new string[] { "int", "int", "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotExist[i] = Convert.ToInt32(requestAnswer[3][0]);

                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotPowered[i] = 1;
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotEnergyRequired[i] = Convert.ToInt32(requestAnswer[0][0]);
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotDamage[i] = Convert.ToInt32(requestAnswer[1][0]);
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotReloadTime[i] = Convert.ToInt32(requestAnswer[2][0]);
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotExist[i] = 0;
                }
            }

            sessionsBattle1v1AI[newBattleID].players[idInArray].playerReady = 1;

            //   Console.WriteLine("DEBUG Session1v1AILoadAI - 8  ");
            // Crew // TO CORRECT
            // sessionsBattle1v1AI[newBattleID].aICrewExist[0] = 1;
            // sessionsBattle1v1AI[newBattleID].aICrewHealth[0] = 10;
            // sessionsBattle1v1AI[newBattleID].aICrewDamage[0] = 1;


            Console.WriteLine("DEBUG Session1v1AILoadPlayer -idInArray  = " + idInArray);
            Console.WriteLine("DEBUG sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipId -  = " + sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipId);

        }

        // corrected to new class
        static private void SessionStartLoadPlayer(string accountId, int newBattleID, int idInArray)
        {

           // Console.WriteLine("DEBUG Session1v1AILoadPlayer - 1 - " + accountId);

            // get information about ID of all modules and ship
            string queryString = @"SELECT AccountShip.AccountShipId, AccountShip.ShipId, AccountShip.EngineSlot, AccountShip.CockpitSlot, AccountShip.BigSlot1, AccountShip.BigSlot2,
                                     AccountShip.BigSlot3, AccountShip.BigSlot4, AccountShip.BigSlot5, AccountShip.MediumSlot1, AccountShip.MediumSlot2,
                                     AccountShip.MediumSlot3, AccountShip.MediumSlot4, AccountShip.MediumSlot5, AccountShip.SmallSlot1, AccountShip.SmallSlot2,
                                     AccountShip.SmallSlot3, AccountShip.SmallSlot4, AccountShip.SmallSlot5,
                                     AccountShip.Weapon1, AccountShip.Weapon2, AccountShip.Weapon3, AccountShip.Weapon4, AccountShip.Weapon5
                            FROM AccountShip, Garage, Account
							WHERE 
							Account.AccountId = @accountId
                            and Garage.Slot = Account.GarageActiveSlot 
                            and Garage.AccountId = Account.AccountId
                            and AccountShip.AccountShipId = Garage.AccountShipId";
            string[,]  queryParameters = new string[,] { { "accountId", accountId } };
            string[]  stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            string accountShipId = requestAnswer[0][0];
            string shipId = requestAnswer[1][0];
            string engineSlotId = requestAnswer[2][0];
            string cockpitSlotId = requestAnswer[3][0];
            string[] bigSlotId = new string[] { requestAnswer[4][0] , requestAnswer[5][0] , requestAnswer[6][0] , requestAnswer[7][0] , requestAnswer[8][0] };
            string[] mediumSlotId = new string[] { requestAnswer[9][0], requestAnswer[10][0], requestAnswer[11][0], requestAnswer[12][0], requestAnswer[13][0] };
            string[] smallSlotId = new string[] { requestAnswer[14][0], requestAnswer[15][0], requestAnswer[16][0], requestAnswer[17][0], requestAnswer[18][0] };

            string[] weaponSlotId = new string[] { requestAnswer[19][0], requestAnswer[20][0], requestAnswer[21][0], requestAnswer[22][0], requestAnswer[23][0] };


          //  Console.WriteLine("DEBUG Session1v1AILoadPlayer - 2");


            // get information about the ship info
            queryString = @"SELECT Ship.BaseHealth, Ship.BaseEnergy
                             FROM Ship
                             WHERE Ship.ShipId = @shipId";
            queryParameters = new string[,] { { "shipId", shipId } };
            stringType = new string[] { "int", "int"  };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipId = Convert.ToInt32(shipId);
            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipMaxHealth = Convert.ToInt32(requestAnswer[0][0]);
            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipMaxEnergy = Convert.ToInt32(requestAnswer[1][0]);
            sessionsBattle1v1AI[newBattleID].players[idInArray].playerShipFreeEnergy = Convert.ToInt32(requestAnswer[1][0]);

         //   Console.WriteLine("DEBUG Session1v1AILoadPlayer - 3");

         //   Console.WriteLine("DEBUG Session1v1AILoadPlayer - engine slot - " + engineSlotId);
            // get information about the engine slot
            if (engineSlotId != "-1" && engineSlotId != "0")
            {
                queryString = @"SELECT Engine.Health, Engine.Energy, Engine.EngineId
                            FROM Engine, AccountItem, Item
                             WHERE AccountItem.ItemId = Item.ItemId 
                             and Item.EngineId = Engine.EngineId 
                             and AccountItem.AccountItemId = @engineId";
                queryParameters = new string[,] { { "engineId", engineSlotId } };
                stringType = new string[] { "int", "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[0] = Convert.ToInt32(requestAnswer[2][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[0] = 0;
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[0] = "engine";
            }
            else if(engineSlotId == "0")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[0] = 0;
            }
            else if(engineSlotId == "-1")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[0] = -1;
            }

          //  Console.WriteLine("DEBUG Session1v1AILoadPlayer - 4");
            // get information about the cockpit slot
            if (cockpitSlotId != "-1" && cockpitSlotId != "0")
            {
                queryString = @"SELECT Cockpit.Health, Cockpit.Energy, Cockpit.CockpitId
                                FROM Cockpit, AccountItem, Item
                                WHERE AccountItem.ItemId = Item.ItemId
                                and Item.CockpitId = Cockpit.CockpitId 
                                and AccountItem.AccountItemId = @cockpitId";
                queryParameters = new string[,] { { "cockpitId", cockpitSlotId } };
                stringType = new string[] { "int", "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[1] = Convert.ToInt32(requestAnswer[2][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[1] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[1] = 0;
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[1] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[1] = "cockpit";
            }
            else if(cockpitSlotId == "0")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[1] = 0;
            }
            else if (cockpitSlotId == "-1")
            {
                sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[1] = -1;
            }


          //  Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5");
            // get information about the bigslots
            for (int i = 0; i < bigSlotId.Length; i++)
            {
                if (bigSlotId[i] != "-1" && bigSlotId[i] != "0") 
                {
                    queryString = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId, BigSlot.BigSlotId
                                FROM BigSlot, AccountItem, Item
                                WHERE AccountItem.ItemId = Item.ItemId
                                and BigSlot.BigSlotId = Item.BigSlotId 
                                and AccountItem.AccountItemId = @bigSlotId";
                    queryParameters = new string[,] { { "bigSlotId", bigSlotId[i] } };
                    stringType = new string[] { "int", "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i+2] = Convert.ToInt32(requestAnswer[2][0]);

               //     Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5.5");
                    //get information if shield or weaponcontrol
                    if (requestAnswer[0][0] != "0") //shield
                    {
                    //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5.51 - " + requestAnswer[0][0]);

                        queryString = @"SELECT Shield.Heath, Shield.Energy, Shield.Capacity, Shield.RechargeTime, Shield.RechargeRate
                                        FROM Shield
                                        WHERE Shield.ShieldId = @shieldId";
                        queryParameters = new string[,] { { "shieldId", requestAnswer[0][0] } };
                        stringType = new string[] { "int", "int", "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5.5111");
                        //---------------------


                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[i + 2] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[i + 2] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[i + 2] = "shield";

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldCapacity[i + 2] = Convert.ToInt32(requestAnswer[2][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldCurrentCapacity[i + 2] = Convert.ToInt32(requestAnswer[2][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldRechargeTime[i + 2] = Convert.ToInt32(requestAnswer[3][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldRechargeCurrentTime[i + 2] = Convert.ToInt32(requestAnswer[3][0]); ;
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldRechargeRate[i + 2] = Convert.ToInt32(requestAnswer[4][0]);

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSumShieldCapacity += Convert.ToInt32(requestAnswer[2][0]);

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotAdditionalInfoToClient[i + 2] = sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotShieldCurrentCapacity[i + 2];
                    //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5.511");
                    }
                    else if (requestAnswer[1][0] != "0") // weapon control
                    {
                      //  Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5.52");

                        queryString = @"SELECT WeaponContol.Health, WeaponContol.Energy, WeaponContol.AmountOfWeapons
                        FROM WeaponContol
                        WHERE WeaponContol.WeaponControlId = @weaponControlId";
                        queryParameters = new string[,] { { "weaponControlId", requestAnswer[1][0] } };
                        stringType = new string[] { "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotHealth[i + 2] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotPowered[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotEnergyRequired[i + 2] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotType[i + 2] = "weaponcontrol";
                        sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotWeaponControlAmountOfWeapons[i + 2] = Convert.ToInt32(requestAnswer[2][0]);

                    //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 5.522");
                    }
                }
                else if (bigSlotId[i] == "0")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 2] = 0;
                }else if (bigSlotId[i] == "-1")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 2] = -1;
                }
            }

         //   Console.WriteLine("DEBUG Session1v1AILoadPlayer - 6");
            // get information about the middleslots
            for (int i = 0; i < mediumSlotId.Length; i++)
            {
                if (mediumSlotId[i] != "-1" && mediumSlotId[i] != "0") 
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 7] = 1;
                }
                else if(mediumSlotId[i] == "0")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 7] = 0;
                }
                else if (mediumSlotId[i] == "-1")
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerSlotExist[i + 7] = -1;
                }
            }

         //   Console.WriteLine("DEBUG Session1v1AILoadPlayer - 7");
            // get information about the weapons
            for (int i = 0; i < weaponSlotId.Length; i++)
            {
              //  Console.WriteLine("DEBUG Session1v1AILoadPlayer - 7 - 1 - 0 - weaponSlotId - " + weaponSlotId[i] + " " + i);

                if (weaponSlotId[i] != "-1" && weaponSlotId[i] != "0")
                {
                //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 7 - 1 - " + weaponSlotId[i]);



                    queryString = @"SELECT Weapon.Energy, Weapon.Damage, Weapon.ReloadTime, Weapon.WeaponId
                                  FROM Weapon, AccountItem, Item
                                 WHERE AccountItem.ItemId = Item.ItemId 
                                and Weapon.WeaponId = Item.WeaponId 
                                and AccountItem.AccountItemId = @weaponId";
                    queryParameters = new string[,] { { "weaponId", weaponSlotId[i] } };
                    stringType = new string[] { "int", "int", "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotExist[i] = Convert.ToInt32(requestAnswer[3][0]);

                    // sessionsBattle1v1AI[newBattleID].playerWeaponSlotPowered[i] = 0;
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotEnergyRequired[i] = Convert.ToInt32(requestAnswer[0][0]);
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotDamage[i] = Convert.ToInt32(requestAnswer[1][0]);
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotReloadTime[i] = Convert.ToInt32(requestAnswer[2][0]);

                //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 711 ");
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].players[idInArray].playerWeaponSlotExist[i] = 0;
                 //   Console.WriteLine("DEBUG Session1v1AILoadPlayer - 711notexist ");
                }

              //  Console.WriteLine("DEBUG Session1v1AILoadPlayer - 711end ");
            }


        //    Console.WriteLine("DEBUG Session1v1AILoadPlayer - 8");
            // Crew // TO CORRECT
         //   sessionsBattle1v1AI[newBattleID].playerCrewExist[0] = 1;
          //  sessionsBattle1v1AI[newBattleID].playerCrewHealth[0] = 10;
          //  sessionsBattle1v1AI[newBattleID].playerCrewDamage[0] = 1;

            Console.WriteLine("DEBUG Session1v1AILoadPlayer -  = " + idInArray);

        }



    }


}
