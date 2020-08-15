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
        private static Dictionary<int, Battle1v1AI> sessionsBattle1v1AI = new Dictionary<int, Battle1v1AI>();

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
            //sessionsBattle1v1AI.Add(123, new Battle1v1AI());
            //sessionsBattle1v1AI[123].toStart = 1;
            //sessionsBattle1v1AI[123].playerWeapon1Damage = 6;
            //sessionsBattle1v1AI[123].aiWeapon1Damage = 6;
            //sessionsBattle1v1AI[123].aiWeapon1ReloadCurrent = 4500;
            //sessionsBattle1v1AI[123].playerWeapon1ReloadCurrent = 4600;
            //sessionsBattle1v1AI[123].playerHealthCurrent = 120;
            //sessionsBattle1v1AI[123].aiHealthCurrent = 120;
            //sessionsBattle1v1AI[123].playerWeapon1ReloadTime = 4600;
            //sessionsBattle1v1AI[123].aiWeapon1ReloadTime = 4500;

            //sessionsBattle1v1AI.Add(321, new Battle1v1AI());
            //sessionsBattle1v1AI[321].toStart = 1;
            //sessionsBattle1v1AI[321].playerWeapon1Damage = 4;
            //sessionsBattle1v1AI[321].aiWeapon1Damage = 4;
            //sessionsBattle1v1AI[321].aiWeapon1ReloadCurrent = 5000;
            //sessionsBattle1v1AI[321].playerWeapon1ReloadCurrent = 5000;
            //sessionsBattle1v1AI[321].playerHealthCurrent = 100;
            //sessionsBattle1v1AI[321].aiHealthCurrent = 100;
            //sessionsBattle1v1AI[321].playerWeapon1ReloadTime = 5000;
            //sessionsBattle1v1AI[321].aiWeapon1ReloadTime = 5000;
            //Update DB (closed = 1) - close battle in DB


            while (true)  // check dictionary with battle's all the time for if need to start battle or not
            {
                try 
                {
                    // check if battle need to be started
                    foreach (int battleSessionId in sessionsBattle1v1AI.Keys)
                    {
                        if (sessionsBattle1v1AI[battleSessionId].toStart == 1 && sessionsBattle1v1AI[battleSessionId].started == 0 && sessionsBattle1v1AI[battleSessionId].playerReady == 1)
                        {
                            // Process battle
                            sessionsBattle1v1AI[battleSessionId].started = 1;
                            Task.Run(() => {
                                Console.WriteLine("battle session number - " + battleSessionId + " is started");



                                // set start health
                                sessionsBattle1v1AI[battleSessionId].SetStartHealth();


                                //update battle session status every 50ms
                                while (sessionsBattle1v1AI[battleSessionId].finished != 1)
                                {


                                    //  -------------------------- Battle process -----------------------------
                                    
                                    // RELOAD
                                    sessionsBattle1v1AI[battleSessionId].ReloadAllWeaponsPerTick();

                                    // ATTACK dummy class
                                    sessionsBattle1v1AI[battleSessionId].AttackDummyClass();




                                    // check if someone dead
                                    if (sessionsBattle1v1AI[battleSessionId].playerHealthCurrent <= 0) {
                                        Console.WriteLine("player dead in  session number - " + battleSessionId + "  and it finished");
                                        sessionsBattle1v1AI[battleSessionId].finished = 1;
                                    }
                                    if (sessionsBattle1v1AI[battleSessionId].aiHealthCurrent <= 0)
                                    {
                                        Console.WriteLine("ai dead in  session number - " + battleSessionId + "  and it finished");
                                        sessionsBattle1v1AI[battleSessionId].finished = 1;
                                    }

                                    //------------------------- end of battle process --------------------------
                                    


                                    // -----------------------------------------------------------comments
                                    //  Console.WriteLine("reload player - "+ sessionsBattle1v1AI[battleSessionId].playerWeapon1ReloadCurrent);
                                    //  Console.WriteLine("reload ai - "+ sessionsBattle1v1AI[battleSessionId].aiWeapon1ReloadCurrent);
                                    //  Console.WriteLine("health player - "+ sessionsBattle1v1AI[battleSessionId].playerHealthCurrent);
                                    //  Console.WriteLine("health ai - "+ sessionsBattle1v1AI[battleSessionId].aiHealthCurrent);
                                    //--------------------------------------------------------------------


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
                catch 
                {
                    Console.WriteLine("BattleProcess main loop error");
                }
                Thread.Sleep(50); // check dictionary every 50 ms (if 0 = 25% processor; if >1 = ~1% processor
            }
        }

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
                Byte[] bytes = new Byte[64]; // 64 symbols
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

                                    // Send back a response.
                                    stream.Write(msg, 0, msg.Length);
                                   // Console.WriteLine("Sent: {0}", dataToSend);
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
                Console.WriteLine("garage manipulation");
                // Checking if it is real player
                string playerId = recievedMessage[1];
                string sessionToken = recievedMessage[2];
                string codeActivity = recievedMessage[3];


                // Authorize player

                if (CacheAuthorization(playerId, sessionToken))
                {
                    // define that modules does not exist in the ship as start 
                    // modules for answer start information
                    int engineSlot = -1;
                    int cockpitSlot = -1;
                    int bigSlot1 = -1;
                    int bigSlot2 = -1;
                    int bigSlot3 = -1;
                    int bigSlot4 = -1;
                    int bigSlot5 = -1;
                    int bigSlot1Type = 0;
                    int bigSlot2Type = 0;
                    int bigSlot3Type = 0;
                    int bigSlot4Type = 0;
                    int bigSlot5Type = 0;
                    int mediumSlot1 = -1;
                    int mediumSlot2 = -1;
                    int mediumSlot3 = -1;
                    int mediumSlot4 = -1;
                    int mediumSlot5 = -1;
                    int smallSlot1 = -1;
                    int smallSlot2 = -1;
                    int smallSlot3 = -1;
                    int smallSlot4 = -1;
                    int smallSlot5 = -1;
                    int weapon1 = -1;
                    int weapon2 = -1;
                    int weapon3 = -1;
                    int weapon4 = -1;
                    int weapon5 = -1;

                    int slot1Ship = -1; // what id ship in slot \activeSlot+0
                    int slot2Ship = -1; // what id ship in slot \activeSlot+1
                    int slot3Ship = -1; // what id ship in slot \activeSlot+2



                    // for DB 
                    List<int> accountShipId = new List<int>();
                    List<int> slots = new List<int>();
                    List<int> shipsID = new List<int>();



                    if (codeActivity == "0")
                    {
                        //Console.WriteLine("Code activity - " + codeActivity);

                        using var connectionToDB = new SQLiteConnection(connectionToDBString);
                        connectionToDB.Open();

                        // getting ships models from the players garage
                        string stm1 = "SELECT Garage.slot, Ship.ShipId, AccountShip.AccountShipId FROM Garage, Ship, AccountShip WHERE Garage.AccountId = @playerID AND Garage.AccountShipId = AccountShip.AccountShipId AND AccountShip.ShipId = Ship.ShipId ORDER BY Garage.slot ASC";
                        using var cmd1 = new SQLiteCommand(stm1, connectionToDB);
                        cmd1.Parameters.AddWithValue("@playerID", playerId);

                        try
                        {
                            using SQLiteDataReader rdr1 = cmd1.ExecuteReader();
                            while (rdr1.Read())
                            {
                                slots.Add(rdr1.GetInt32(0));
                                shipsID.Add(rdr1.GetInt32(1));
                                accountShipId.Add(rdr1.GetInt32(2));
                            }


                            // select first three slots with ship's ID
                            if (slots.Count == 1)
                            {
                                slot1Ship = shipsID[0];
                            }
                            else if (slots.Count == 2)
                            {
                                slot1Ship = shipsID[0];
                                slot2Ship = shipsID[1];
                            }
                            else if (slots.Count >= 3)
                            {
                                slot1Ship = shipsID[0];
                                slot2Ship = shipsID[1];
                                slot3Ship = shipsID[2];
                            }




                            //----------------------------------------------------------

                            // Get information about the ship that in the slot (modules etc)
                            stm1 = @"SELECT AccountShip.AccountShipId, AccountShip.EngineSlot, AccountShip.CockpitSlot, AccountShip.BigSlot1, AccountShip.BigSlot2,
                                     AccountShip.BigSlot3, AccountShip.BigSlot4, AccountShip.BigSlot5, AccountShip.MediumSlot1, AccountShip.MediumSlot2,
                                     AccountShip.MediumSlot3, AccountShip.MediumSlot4, AccountShip.MediumSlot5, AccountShip.SmallSlot1, AccountShip.SmallSlot2,
                                     AccountShip.SmallSlot3, AccountShip.SmallSlot4, AccountShip.SmallSlot5,
                                     AccountShip.Weapon1, AccountShip.Weapon2, AccountShip.Weapon3, AccountShip.Weapon4, AccountShip.Weapon5
                            FROM AccountShip WHERE AccountShip.AccountShipId = @accountShipId";
                            using var cmd2 = new SQLiteCommand(stm1, connectionToDB);
                            cmd2.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                            try 
                            {
                                using SQLiteDataReader rdr2 = cmd2.ExecuteReader();
                                while (rdr2.Read())
                                {
                                    engineSlot = rdr2.GetInt32(1);
                                    cockpitSlot = rdr2.GetInt32(2);
                                    bigSlot1 = rdr2.GetInt32(3);
                                    bigSlot2 = rdr2.GetInt32(4);
                                    bigSlot3 = rdr2.GetInt32(5);
                                    bigSlot4 = rdr2.GetInt32(6);
                                    bigSlot5 = rdr2.GetInt32(7);
                                    mediumSlot1 = rdr2.GetInt32(8);
                                    mediumSlot2 = rdr2.GetInt32(9);
                                    mediumSlot3 = rdr2.GetInt32(10);
                                    mediumSlot4 = rdr2.GetInt32(11);
                                    mediumSlot5 = rdr2.GetInt32(12);
                                    smallSlot1 = rdr2.GetInt32(13);
                                    smallSlot2 = rdr2.GetInt32(14);
                                    smallSlot3 = rdr2.GetInt32(15);
                                    smallSlot4 = rdr2.GetInt32(16);
                                    smallSlot5 = rdr2.GetInt32(17);
                                    weapon1 = rdr2.GetInt32(18);
                                    weapon2 = rdr2.GetInt32(19);
                                    weapon3 = rdr2.GetInt32(20);
                                    weapon4 = rdr2.GetInt32(21);
                                    weapon5 = rdr2.GetInt32(22);
                                    Console.WriteLine("AccountShipId - " + rdr2.GetInt32(0));
                                    Console.WriteLine("EngineSlot - " + rdr2.GetInt32(1));
                                    Console.WriteLine("CockpitSLot - " + rdr2.GetInt32(2));
                                    Console.WriteLine("BigSLot1 - " + rdr2.GetInt32(3));
                                    Console.WriteLine("BigSLot2 - " + rdr2.GetInt32(4));
                                    Console.WriteLine("BigSLot3 - " + rdr2.GetInt32(5));
                                    Console.WriteLine("BigSLot4 - " + rdr2.GetInt32(6));
                                    Console.WriteLine("BigSLot5 - " + rdr2.GetInt32(7));
                                    Console.WriteLine("MediumSlot1 - " + rdr2.GetInt32(8));
                                    Console.WriteLine("MediumSlot2 - " + rdr2.GetInt32(9));
                                    Console.WriteLine("MediumSlot3 - " + rdr2.GetInt32(10));
                                    Console.WriteLine("MediumSlot4 - " + rdr2.GetInt32(11));
                                    Console.WriteLine("MediumSlot5 - " + rdr2.GetInt32(12));
                                    Console.WriteLine("SmallSlot1 - " + rdr2.GetInt32(13));
                                    Console.WriteLine("SmallSlot2 - " + rdr2.GetInt32(14));
                                    Console.WriteLine("SmallSlot3 - " + rdr2.GetInt32(15));
                                    Console.WriteLine("SmallSlot4 - " + rdr2.GetInt32(16));
                                    Console.WriteLine("SmallSlot5 - " + rdr2.GetInt32(17));
                                    Console.WriteLine("Weapon1 - " + rdr2.GetInt32(18));
                                    Console.WriteLine("Weapon2 - " + rdr2.GetInt32(19));
                                    Console.WriteLine("Weapon3 - " + rdr2.GetInt32(20));
                                    Console.WriteLine("Weapon4 - " + rdr2.GetInt32(21));
                                    Console.WriteLine("Weapon5 - " + rdr2.GetInt32(22));
                                }

                            }
                            catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }

                            // check system if some module installed - what id of the module 
                            if (engineSlot > 0) 
                            {
                            // get infromation about Engine
                            stm1 = @"SELECT Engine.EngineId
                                    FROM AccountShip, AccountItem, Engine
                                    WHERE AccountShip.AccountShipId = @accountShipId 
                                    and AccountShip.EngineSlot = AccountItem.AccountItemId 
                                    and AccountItem.EngineId = Engine.EngineId";
  
                            using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                            cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                  using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                  while (rdr2.Read())
                                     {
                                        engineSlot = rdr2.GetInt32(0);
                                     }
                                 }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            if (cockpitSlot > 0) 
                            {
                                // get infromation about cockpit
                                stm1 = @"SELECT Cockpit.CockpitId
                                    FROM AccountShip, AccountItem, Cockpit
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.CockpitSlot = AccountItem.AccountItemId 
                                    and AccountItem.CockpitId = Cockpit.CockpitId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                    using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                    while (rdr2.Read())
                                    {
                                        cockpitSlot = rdr2.GetInt32(0);
                                    }
                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            
                            if (bigSlot1 > 0)
                            {
                                int shieldId = 0;
                                int weaponControlId = 0;
                                // get infromation about bigSlot1
                                stm1 = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                    FROM AccountShip, AccountItem, BigSlot
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.BigSlot1 = AccountItem.AccountItemId 
                                    and AccountItem.BigSlotId = BigSlot.BigSlotId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                    using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                    while (rdr2.Read())
                                    {
                                        shieldId = rdr2.GetInt32(0);
                                        weaponControlId = rdr2.GetInt32(1);
                                    }

                                    if (shieldId > 0)
                                    {
                                        bigSlot1Type = 1;
                                        stm1 = @"SELECT Shield.ShieldId
                                    FROM Shield
                                    WHERE Shield.ShieldId = @shieldId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@shieldId", shieldId);
                                        try 
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot1 = rdr3.GetInt32(0);
                                            }
                                        } catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    } 
                                    else if (weaponControlId > 0) 
                                    {
                                        bigSlot1Type = 2;
                                        stm1 = @"SELECT WeaponContol.WeaponControlId
                                    FROM WeaponContol
                                    WHERE WeaponContol.WeaponControlId = @weaponControlId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@weaponControlId", weaponControlId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot1 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }


                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }

                            if (bigSlot2 > 0)
                            {
                                int shieldId = 0;
                                int weaponControlId = 0;
                                // get infromation about bigSlot1
                                stm1 = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                    FROM AccountShip, AccountItem, BigSlot
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.BigSlot2 = AccountItem.AccountItemId 
                                    and AccountItem.BigSlotId = BigSlot.BigSlotId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                    using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                    while (rdr2.Read())
                                    {
                                        shieldId = rdr2.GetInt32(0);
                                        weaponControlId = rdr2.GetInt32(1);
                                    }

                                    if (shieldId > 0)
                                    {
                                        bigSlot1Type = 1;
                                        stm1 = @"SELECT Shield.ShieldId
                                    FROM Shield
                                    WHERE Shield.ShieldId = @shieldId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@shieldId", shieldId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot2 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }
                                    else if (weaponControlId > 0)
                                    {
                                        bigSlot1Type = 2;
                                        stm1 = @"SELECT WeaponContol.WeaponControlId
                                    FROM WeaponContol
                                    WHERE WeaponContol.WeaponControlId = @weaponControlId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@weaponControlId", weaponControlId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot2 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }


                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                           
                            if (bigSlot3 > 0) 
                            {
                                int shieldId = 0;
                                int weaponControlId = 0;
                                // get infromation about bigSlot1
                                stm1 = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                    FROM AccountShip, AccountItem, BigSlot
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.BigSlot3 = AccountItem.AccountItemId 
                                    and AccountItem.BigSlotId = BigSlot.BigSlotId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                    using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                    while (rdr2.Read())
                                    {
                                        shieldId = rdr2.GetInt32(0);
                                        weaponControlId = rdr2.GetInt32(1);
                                    }

                                    if (shieldId > 0)
                                    {
                                        bigSlot1Type = 1;
                                        stm1 = @"SELECT Shield.ShieldId
                                    FROM Shield
                                    WHERE Shield.ShieldId = @shieldId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@shieldId", shieldId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot3 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }
                                    else if (weaponControlId > 0)
                                    {
                                        bigSlot1Type = 2;
                                        stm1 = @"SELECT WeaponContol.WeaponControlId
                                    FROM WeaponContol
                                    WHERE WeaponContol.WeaponControlId = @weaponControlId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@weaponControlId", weaponControlId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot3 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }


                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            
                            if (bigSlot4 > 0) 
                            {
                                int shieldId = 0;
                                int weaponControlId = 0;
                                // get infromation about bigSlot1
                                stm1 = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                    FROM AccountShip, AccountItem, BigSlot
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.BigSlot4 = AccountItem.AccountItemId 
                                    and AccountItem.BigSlotId = BigSlot.BigSlotId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                    using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                    while (rdr2.Read())
                                    {
                                        shieldId = rdr2.GetInt32(0);
                                        weaponControlId = rdr2.GetInt32(1);
                                    }

                                    if (shieldId > 0)
                                    {
                                        bigSlot1Type = 1;
                                        stm1 = @"SELECT Shield.ShieldId
                                    FROM Shield
                                    WHERE Shield.ShieldId = @shieldId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@shieldId", shieldId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot4 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }
                                    else if (weaponControlId > 0)
                                    {
                                        bigSlot1Type = 2;
                                        stm1 = @"SELECT WeaponContol.WeaponControlId
                                    FROM WeaponContol
                                    WHERE WeaponContol.WeaponControlId = @weaponControlId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@weaponControlId", weaponControlId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot4 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }


                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            
                            if (bigSlot5 > 0) 
                            {
                                int shieldId = 0;
                                int weaponControlId = 0;
                                // get infromation about bigSlot1
                                stm1 = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                    FROM AccountShip, AccountItem, BigSlot
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.BigSlot5 = AccountItem.AccountItemId 
                                    and AccountItem.BigSlotId = BigSlot.BigSlotId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                try
                                {
                                    using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                    while (rdr2.Read())
                                    {
                                        shieldId = rdr2.GetInt32(0);
                                        weaponControlId = rdr2.GetInt32(1);
                                    }

                                    if (shieldId > 0)
                                    {
                                        bigSlot1Type = 1;
                                        stm1 = @"SELECT Shield.ShieldId
                                    FROM Shield
                                    WHERE Shield.ShieldId = @shieldId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@shieldId", shieldId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot5 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }
                                    else if (weaponControlId > 0)
                                    {
                                        bigSlot1Type = 2;
                                        stm1 = @"SELECT WeaponContol.WeaponControlId
                                    FROM WeaponContol
                                    WHERE WeaponContol.WeaponControlId = @weaponControlId";
                                        using var cmd4 = new SQLiteCommand(stm1, connectionToDB);
                                        cmd4.Parameters.AddWithValue("@weaponControlId", weaponControlId);
                                        try
                                        {
                                            using SQLiteDataReader rdr3 = cmd4.ExecuteReader();
                                            while (rdr3.Read())
                                            {
                                                bigSlot5 = rdr3.GetInt32(0);
                                            }
                                        }
                                        catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                                    }


                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }

                            if (mediumSlot1 > 0) { }
                            if (mediumSlot2 > 0) { }
                            if (mediumSlot3 > 0) { }
                            if (mediumSlot4 > 0) { }
                            if (mediumSlot5 > 0) { }
                            if (smallSlot1 > 0) { }
                            if (smallSlot2 > 0) { }
                            if (smallSlot3 > 0) { }
                            if (smallSlot4 > 0) { }
                            if (smallSlot5 > 0) { }

                            if (weapon1 > 0) 
                            {
                                stm1 = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon1 = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                try 
                                { 
                                    while (rdr2.Read())
                                     {
                                        weapon1 = rdr2.GetInt32(0);
                                     }
                                 } catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            if (weapon2 > 0) 
                            {
                                stm1 = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon2 = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                try
                                {
                                    while (rdr2.Read())
                                    {
                                        weapon2 = rdr2.GetInt32(0);
                                    }
                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            if (weapon3 > 0) 
                            {
                                stm1 = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon3 = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                try
                                {
                                    while (rdr2.Read())
                                    {
                                        weapon3 = rdr2.GetInt32(0);
                                    }
                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            if (weapon4 > 0) 
                            {
                                stm1 = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon4 = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                try
                                {
                                    while (rdr2.Read())
                                    {
                                        weapon4 = rdr2.GetInt32(0);
                                    }
                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            if (weapon5 > 0) 
                            {
                                stm1 = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon5 = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";

                                using var cmd3 = new SQLiteCommand(stm1, connectionToDB);
                                cmd3.Parameters.AddWithValue("@accountShipId", accountShipId[0]);
                                using SQLiteDataReader rdr2 = cmd3.ExecuteReader();
                                try
                                {
                                    while (rdr2.Read())
                                    {
                                        weapon5 = rdr2.GetInt32(0);
                                    }
                                }
                                catch (InvalidCastException e) { Console.WriteLine("ERROR - " + e); }
                            }
                            //---------------------------------------------------





                        }
                        catch (InvalidCastException e)
                        {
                            Console.WriteLine("ERROR - " + e);
                        }

                        connectionToDB.Close();


                        // Anwer to client
                        answerToClient = slot1Ship + ";" + slot2Ship + ";" + slot3Ship + ";" + engineSlot  + ";" + cockpitSlot  + ";" + bigSlot1Type  + ";" + bigSlot1+ ";" + bigSlot2Type  + ";" + bigSlot2+ ";" + bigSlot3Type  + ";" + bigSlot3+ ";" + bigSlot4Type  + ";" + bigSlot4 + ";" + bigSlot5Type  + ";" + bigSlot5 + ";" + mediumSlot1  + ";" + mediumSlot2  + ";" + mediumSlot3  + ";" + mediumSlot4  + ";" + mediumSlot5  + ";" + smallSlot1   + ";" + smallSlot2   + ";" + smallSlot3   + ";" + smallSlot4   + ";" + smallSlot5 + ";" + weapon1 + ";" + weapon2+ ";" + weapon3+ ";" + weapon4+ ";" + weapon5;
                       // answerToClient = slot1Ship + ";" + slot2Ship + ";" + slot3Ship;
                    }
                    else if (codeActivity == "1")
                    {
                        Console.WriteLine("Battle 1v1 AI");
                        string sessionID = StartSession1v1AI(Convert.ToInt32(playerId));

                        // add information about the sessionBattleID to cache
                        if (playerCache.ContainsKey(playerId))
                        {
                            playerCache[playerId][1] = sessionID;
                        }

                        //TEST
                        answerToClient = sessionID;
                    }
                    else if (codeActivity == "2") 
                    {
                        Console.WriteLine("Inventory information ");

                        using var connectionToDB = new SQLiteConnection(connectionToDBString);

                        connectionToDB.Open();

                        string stm1 = "SELECT * FROM AccountItem WHERE AccountItem.AccountId = @playerID";
                        using var cmd1 = new SQLiteCommand(stm1, connectionToDB);
                        cmd1.Parameters.AddWithValue("@playerID", playerId);

                        string answerType = "";
                        string answerItemTypeId = "";
                        try
                        {
                            using SQLiteDataReader rdr1 = cmd1.ExecuteReader();

                            answerToClient = "";
                            while (rdr1.Read())
                            {
                                // 0 - 9 including 
                               // Console.WriteLine("0" + rdr1.GetInt32(0));

                                if (rdr1.GetInt32(4) != 0)
                                {
                                    answerType = "0";
                                    answerItemTypeId = Convert.ToString(rdr1.GetInt32(4));
                                }
                                else if (rdr1.GetInt32(5) != 0) 
                                {
                                    answerType = "1";
                                    answerItemTypeId = Convert.ToString(rdr1.GetInt32(5));
                                }
                                else if (rdr1.GetInt32(6) != 0)
                                {
                                    answerType = "2";
                                    answerItemTypeId = Convert.ToString(rdr1.GetInt32(6));
                                }
                                else if (rdr1.GetInt32(7) != 0)
                                {
                                    answerType = "3";
                                    answerItemTypeId = Convert.ToString(rdr1.GetInt32(7));
                                }
                                else if (rdr1.GetInt32(8) != 0)
                                {
                                    answerType = "4";
                                    answerItemTypeId = Convert.ToString(rdr1.GetInt32(8));
                                }
                                else if (rdr1.GetInt32(9) != 0)
                                {
                                    answerType = "5";
                                    answerItemTypeId = Convert.ToString(rdr1.GetInt32(9));
                                }
                                answerToClient = answerToClient + Convert.ToString(rdr1.GetInt32(0) + ";" + answerType + ";" + answerItemTypeId + ";");
                            }
                            answerToClient = answerToClient.Remove(answerToClient.Length - 1);
                        }
                        catch (InvalidCastException e)
                        {
                            Console.WriteLine("ERROR - " + e);
                        }

                        connectionToDB.Close();
                    }

                }



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
                                // prepare infromation to send for UI
                                int playerShipId = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerShipId;
                                int aiShipId = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].aiShipId;
                                int playerHealthMax = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerHealthMax;
                                int aiHealthMax = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].aiHealthMax;
                                string playerWeapon1Name = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerWeapon1Name;
                                int playerWeapon1Damage = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerWeapon1Damage;
                                int playerWeapon1ReloadTime = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerWeapon1ReloadTime;

                                // answer to client
                                answerToClient = playerShipId + ";" + aiShipId + ";" + playerHealthMax + ";" + aiHealthMax + ";" + playerWeapon1Name + ";" + playerWeapon1Damage + ";" + playerWeapon1ReloadTime;
                            }
                            // Telling server that client is ready and may start battleLoop
                            else if (recievedMessage[4] == "1")
                            {
                                sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerReady = 1;
                                // answer to client
                                answerToClient = "answer for 1 - battle loop is started";
                            }
                            // require information for update UI without action
                            else if (recievedMessage[4] == "2")
                            {
                                int battleTime = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].battleTime;
                                int playerHealthCurrent = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerHealthCurrent;
                                int enemyHealthCurrent = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].aiHealthCurrent;
                                int playerWeapon1ReloadCurrent = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerWeapon1ReloadCurrent;

                                answerToClient = battleTime + ";" + playerHealthCurrent + ";" + enemyHealthCurrent + ";" + playerWeapon1ReloadCurrent;
                            }
                            // require information for update UI WITH action
                            else if (recievedMessage[4] == "3")
                            {
                                // code system of pressed buttons
                                // ????????????????????
                                if (recievedMessage[5] == "0")
                                {
                                    // TEST ONE - pressed only button attack weapon1
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerAttackWeapon();


                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                                else if (recievedMessage[5] == "1")
                                {
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

        static private string ProcessLoginRequest(String[] recievedMessage) {

            string login = recievedMessage[1];
            string password = recievedMessage[2];
            string hashedPasswordFromDB = "";
            string playerId = "";
            string answerToClient = "";

            Console.WriteLine("Recieved login require from " + login);

            string queryString = "SELECT Password  FROM Account where Login ='" + login + "'";
            string stringType = "string";
            hashedPasswordFromDB = RequestToGetSingleValueFromDB(queryString, stringType);

            if (hashedPasswordFromDB == "")
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
                    string enqueryUpdate = "UPDATE Account SET SessionToken = '" + sessionToken + "', GarageActiveSlot = 0  WHERE Login = '" + login + "'";

                    using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

                    try
                    {
                        commandUpdate.ExecuteNonQuery();

                        // get Player ID
                        string stm1 = "SELECT AccountId  FROM Account where Login ='" + login + "' AND SessionToken ='" + sessionToken + "'";
                        using var cmd1 = new SQLiteCommand(stm1, connectionToDB);
                        using SQLiteDataReader rdr1 = cmd1.ExecuteReader();

                        while (rdr1.Read())
                        {
                            playerId = Convert.ToString(rdr1.GetInt32(0));
                        }


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

                        answerToClient = playerId + ";" + sessionToken;

                        Console.WriteLine("Player - " + login + " login SUCCESSEFUL");
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

            Console.WriteLine("!debug! answer to the clien" + answerToClient);
            return answerToClient;
        }


        // Request to DB to recieve value from SINGLE column
        static private string RequestToGetSingleValueFromDB(string queryString, string stringType) {
            string queryResult = "";

            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();
            using var cmd = new SQLiteCommand(queryString, connectionToDB);

            try
            {
                using SQLiteDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {

                    while (reader.Read())
                    {
                        // if requested value in DB is string - get string, if requested value in DB int - get int
                        if (stringType == "string")
                        {
                            queryResult = reader.GetString(0);
                        }
                        else if (stringType == "int")
                        {
                            queryResult = Convert.ToString(reader.GetInt32(0));
                        }
                    }
                }
                else 
                {
                    Console.WriteLine("RequestToGetSingleValueFromDB - No rows found.");
                }
                reader.Close();
            }
            catch
            {
                Console.WriteLine("error with recieveing information from login table RequestToGetSingleValueFromDB"); 
            }
            connectionToDB.Close();

            return queryResult;
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


            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();

            string stm = "SELECT GarageActiveSlot  FROM Account where AccountId ='" + playerID + "' ";
            using var cmd = new SQLiteCommand(stm, connectionToDB);

            using SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                activeSlot = rdr.GetInt32(0);
            }



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



            // Start class with battle parameters
            sessionsBattle1v1AI.Add(newBattleID, new Battle1v1AI());
            Console.WriteLine("new session with number - " + newBattleID + " is started with player -  " + sessionsBattle1v1AI[newBattleID].playerID);

            // LOAD INFORMATION ABOUT PLAYER TO CLASS
            sessionsBattle1v1AI[newBattleID].playerID = playerID;


            //  Load information to the class about the battle that going to start
            //  -----------------------------------------------------
            try
            {
                string stm1 = @"Select Ship.BaseHealth,Ship.BaseEnergy, WeaponContol.Health, WeaponContol.Energy,
		                Weapon.Damage, Weapon.ReloadTime, Weapon.Energy, Weapon.Name, Ship.ShipId
                    FROM Account, Garage, AccountShip, Ship, AccountItem, Weapon, WeaponContol
                    WHERE Account.AccountId = @AccountId 
                            and Garage.Slot = Account.GarageActiveSlot 
                            and Garage.AccountId = Account.AccountId
                            and AccountShip.AccountShipId = Garage.AccountShipId
                            and AccountShip.ShipId = Ship.ShipId
                            and 
                                (
                                (AccountShip.Weapon1 = AccountItem.AccountItemId
                                and AccountItem.WeaponId = Weapon.WeaponId) 
                                or
                                (AccountShip.WeaponControl = AccountItem.AccountItemId
                                and AccountItem.WeaponControlId = WeaponContol.WeaponControlId) 
                                )";


                using var cmd1 = new SQLiteCommand(stm1, connectionToDB);

                //int AccountId = playerID;
                cmd1.Parameters.AddWithValue("@AccountId", playerID);

                try
                {
                    using SQLiteDataReader rdr1 = cmd1.ExecuteReader();
                    rdr1.Read();

                    Console.WriteLine("Ship BaseHealth - " + rdr1.GetInt32(0));
                    Console.WriteLine("Ship BaseEnergy - " + rdr1.GetInt32(1));
                    Console.WriteLine("WeaponContol Health - " + rdr1.GetInt32(2));
                    Console.WriteLine("WeaponContol Energy - " + rdr1.GetInt32(3));
                    Console.WriteLine("Weapon Damage - " + rdr1.GetInt32(4));
                    Console.WriteLine("Weapon ReloadTime - " + rdr1.GetInt32(5));
                    Console.WriteLine("Weapon Energy - " + rdr1.GetInt32(6));
                    Console.WriteLine("Weapon Name - " + rdr1.GetString(7));
                    Console.WriteLine("Ship Id - " + rdr1.GetInt32(8));

                    int shipBaseHealth = rdr1.GetInt32(0);
                    int shipBaseEnergy = rdr1.GetInt32(1);
                    int weaponContolHealth = rdr1.GetInt32(2);
                    int weaponContolEnergy = rdr1.GetInt32(3);
                    int weapon1Damage = rdr1.GetInt32(4);
                    int weapon1ReloadTime = rdr1.GetInt32(5);
                    int weapon1Energy = rdr1.GetInt32(6);
                    string weapon1Name = rdr1.GetString(7);
                    int shipId = rdr1.GetInt32(8);

                    // add informaton to a class - player starting parameters in class
                    sessionsBattle1v1AI[newBattleID].playerHealthMax = shipBaseHealth;
                    sessionsBattle1v1AI[newBattleID].playerEnergyMax = shipBaseEnergy;
                    sessionsBattle1v1AI[newBattleID].playerWeaponControlHealthMax = weaponContolHealth;
                    sessionsBattle1v1AI[newBattleID].playerWeaponControlEnergyRequired = weaponContolEnergy;
                    sessionsBattle1v1AI[newBattleID].playerWeapon1Damage = weapon1Damage;
                    sessionsBattle1v1AI[newBattleID].playerWeapon1ReloadTime = weapon1ReloadTime;
                    sessionsBattle1v1AI[newBattleID].playerWeapon1EnergyRequired = weapon1Energy;
                    sessionsBattle1v1AI[newBattleID].playerWeapon1Name = weapon1Name;
                    sessionsBattle1v1AI[newBattleID].playerShipId = shipId;
                }
                catch (Exception e)
                {
                    Console.WriteLine("error selecting information from the DB about the ship player for class : ", e);
                }


                // LOAD INFORMATION ABOUT AI TO CLASS

                int aiId = 1;     // CHANGE IT WHEN IT WILL BE AN CHOISE FROM PLAYER TO PLAY AGAINST WHAT AI

                sessionsBattle1v1AI[newBattleID].aiId = aiId;


                string aiQuery = @"Select Ship.BaseHealth,Ship.BaseEnergy, WeaponContol.Health, WeaponContol.Energy,
		                Weapon.Damage, Weapon.ReloadTime, Weapon.Energy, Ship.ShipId 
                    FROM Ai, AiShip, Ship, Weapon, WeaponContol
                    WHERE Ai.AiId = @AiId 
                            and Ai.AiShipId = AiShip.AiShipId
                            and AiShip.ShipId = Ship.ShipId
							and
                                (
                                (AiShip.Weapon1 =  Weapon.WeaponId) 
                                or
                                (AiShip.WeaponControl = WeaponContol.WeaponControlId) 
                                )";

                using var cmdAiQuery = new SQLiteCommand(aiQuery, connectionToDB);
                cmdAiQuery.Parameters.AddWithValue("@AiId", aiId);

                try
                {
                    using SQLiteDataReader readerSql = cmdAiQuery.ExecuteReader();
                    readerSql.Read();

                    Console.WriteLine("Ship BaseHealth - " + readerSql.GetInt32(0));
                    Console.WriteLine("Ship BaseEnergy - " + readerSql.GetInt32(1));
                    Console.WriteLine("WeaponContol Health - " + readerSql.GetInt32(2));
                    Console.WriteLine("WeaponContol Energy - " + readerSql.GetInt32(3));
                    Console.WriteLine("Weapon Damage - " + readerSql.GetInt32(4));
                    Console.WriteLine("Weapon ReloadTime - " + readerSql.GetInt32(5));
                    Console.WriteLine("Weapon Energy - " + readerSql.GetInt32(6));
                    Console.WriteLine("Ship Id - " + readerSql.GetInt32(7));

                    int shipBaseHealth = readerSql.GetInt32(0);
                    int shipBaseEnergy = readerSql.GetInt32(1);
                    int weaponContolHealth = readerSql.GetInt32(2);
                    int weaponContolEnergy = readerSql.GetInt32(3);
                    int weapon1Damage = readerSql.GetInt32(4);
                    int weapon1ReloadTime = readerSql.GetInt32(5);
                    int weapon1Energy = readerSql.GetInt32(6);
                    int shipId = readerSql.GetInt32(7);

                    sessionsBattle1v1AI[newBattleID].aiHealthMax = shipBaseHealth;
                    sessionsBattle1v1AI[newBattleID].aiEnergyMax = shipBaseEnergy;
                    sessionsBattle1v1AI[newBattleID].aiWeaponControlHealthMax = weaponContolHealth;
                    sessionsBattle1v1AI[newBattleID].aiWeaponControlEnergyRequired = weaponContolEnergy;
                    sessionsBattle1v1AI[newBattleID].aiWeapon1Damage = weapon1Damage;
                    sessionsBattle1v1AI[newBattleID].aiWeapon1ReloadTime = weapon1ReloadTime;
                    sessionsBattle1v1AI[newBattleID].aiWeapon1EnergyRequired = weapon1Energy;
                    sessionsBattle1v1AI[newBattleID].aiShipId = shipId;
                }
                catch (Exception e)
                {
                    Console.WriteLine("error selecting information from the DB about the ship ai for class : ", e);
                }



                sessionsBattle1v1AI[newBattleID].toStart = 1;
                Console.WriteLine("SESSION TO START - YES - " + sessionsBattle1v1AI[newBattleID].toStart);
            }
            catch {
                Console.WriteLine("Unable to create fill up battle session");
            }


            //  -----------------------------------------------------
            connectionToDB.Close();

            return Convert.ToString(newBattleID);
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


    }


    /*
      Battle1v1AI - class for battle, contain all information about the battle
        and all actions that can be in that battle
    */
    public class Battle1v1AI {

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

        public void SetStartHealth() {
            playerHealthCurrent = playerHealthMax;
            aiHealthCurrent = aiHealthMax;
        }

        public void SetStartReload()
        {
            playerWeapon1ReloadCurrent = playerWeapon1ReloadTime;
            aiWeapon1ReloadCurrent = aiWeapon1ReloadTime;
        }


        public void ReloadAllWeaponsPerTick() {
            int reloadOneTick = 50; // ms

            // reload of the player weapon
            if (playerWeapon1ReloadCurrent > 0)
            {
                playerWeapon1ReloadCurrent -= reloadOneTick;

            }
            else if (playerWeapon1ReloadCurrent <= 0)
            {
                playerWeapon1ReloadCurrent = 0;
            }


            // reload of the ai weapon
            if (aiWeapon1ReloadCurrent > 0)
            {
                aiWeapon1ReloadCurrent -= reloadOneTick;
            }
            else if (aiWeapon1ReloadCurrent <= 0)
            {
                aiWeapon1ReloadCurrent = 0;
            }
        }

        public void AttackDummyClass() {
            //ai attack player
            if (aiWeapon1ReloadCurrent == 0)
            {
                playerHealthCurrent -= aiWeapon1Damage;
                aiWeapon1ReloadCurrent = aiWeapon1ReloadTime;
            }
        }

        public void PlayerAttackWeapon() {
            // player attack AI
            if (playerWeapon1ReloadCurrent == 0)
            {
                aiHealthCurrent -= playerWeapon1Damage;
                playerWeapon1ReloadCurrent = playerWeapon1ReloadTime;
            }
        }

        //Variables

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

        public int aiShipId { get; set; } 


        //-------------------- Player -------------------------

        // Basic ship parameters
        public int playerHealthMax { get; set; }
        public int playerHealthCurrent { get; set; }
        public int playerEnergyMax { get; set; }
        public int playerEnergyFree { get; set; }

        // Weapon Control
        public int playerWeaponControlHealthMax { get; set; }
        public int playerWeaponControlHealthCurrent { get; set; }
        public int playerWeaponControlEnergyRequired { get; set; }
        public int playerWeaponControlEnergyCurrent { get; set; }

        // Weapon 1 
        public string playerWeapon1Name { get; set; }
        public int playerWeapon1Damage { get; set; }
        public int playerWeapon1ReloadTime { get; set; }
        public int playerWeapon1ReloadCurrent { get; set; }
        public int playerWeapon1EnergyRequired { get; set; }
        public int playerWeapon1EnergyCurrent { get; set; }




        //-------------------- AI -------------------------
        // Basic ship parameters
        public int aiHealthMax { get; set; }
        public int aiHealthCurrent { get; set; }
        public int aiEnergyMax { get; set; }
        public int aiEnergyFree { get; set; }

        // Weapon Control
        public int aiWeaponControlHealthMax { get; set; }
        public int aiWeaponControlHealthCurrent { get; set; }
        public int aiWeaponControlEnergyRequired { get; set; }
        public int aiWeaponControlEnergyCurrent { get; set; }

        // Weapon 1 
        public int aiWeapon1Damage { get; set; }
        public int aiWeapon1ReloadTime { get; set; }
        public int aiWeapon1ReloadCurrent { get; set; }
        public int aiWeapon1EnergyRequired { get; set; }
        public int aiWeapon1EnergyCurrent { get; set; }


    }

}
