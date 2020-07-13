﻿/*
 * This file is part of AceQL C# Client SDK.
 * AceQL C# Client SDK: Remote SQL access over HTTP with AceQL HTTP.                                 
 * Copyright (C) 2017,  KawanSoft SAS
 * (http://www.kawansoft.com). All rights reserved.                                
 *                                                                               
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using AceQL.Client.Api;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AceQL.Client.Tests
{
    /// <summary>
    /// Tests AceQL client SDK by calling all APIs.
    /// </summary>
    class AceQLTest
    {
        private const string ACEQL_PCL_FOLDER = "AceQLPclFolder";

        public static void TheMain(string[] args)
        {
            try
            {
                DoIt(args).Wait();
                //DoIt(args).GetAwaiter().GetResult();

                AceQLConsole.WriteLine();
                AceQLConsole.WriteLine("Press enter to close....");
                Console.ReadLine();
            }
            catch (Exception exception)
            {
                AceQLConsole.WriteLine(exception.ToString());
                AceQLConsole.WriteLine(exception.StackTrace);
                AceQLConsole.WriteLine("Press enter to close...");
                Console.ReadLine();
            }
        }

        static async Task DoIt(string[] args)
        {
            /*
            await HttpClientLoopTest.Test();
            bool doReturn = true;
            if (doReturn) return;
            */

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            string serverUrlLocalhost = "http://localhost:9090/aceql";
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            string serverUrlLocalhostTomcat = "http://localhost:8080/aceql-test/aceql";
#pragma warning restore CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            string serverUrlLinuxNoSSL = "http://www.runsafester.net:8081/aceql";
            string serverUrlLinux = "https://www.aceql.com:9443/aceql";
#pragma warning restore CS0219 // Variable is assigned but its value is never used

            string server = serverUrlLinuxNoSSL;
            string database = "sampledb";
            string username = "user1";
            string password = "password1";

            bool useLdapAuth = false;
            //LDAP Tests
            if (useLdapAuth)
            {
                AceQLConsole.WriteLine("WARNING: using LDAP!");
                username = "cn=read-only-admin,dc=example,dc=com";
                //username = "CN=L. Eagle,O=Sue\\2C Grabbit and Runn,C=GB";
                password = "password";
            }

            //customer_id integer NOT NULL,
            //customer_title character(4),
            //fname character varying(32),
            //lname character varying(32) NOT NULL,
            //addressline character varying(64),
            //town character varying(32),
            //zipcode character(10) NOT NULL,
            //phone character varying(32),

            string connectionString = $"Server={server}; Database={database}; ";

            Boolean doItWithCredential = false;

            if (!doItWithCredential)
            {
                connectionString += $"Username={username}; Password={password}; EnableDefaultSystemAuthentication=True";

                AceQLConsole.WriteLine("Using connectionString with Username & Password: " + connectionString);

                // Make sure connection is always closed to close and release server connection into the pool
                using (AceQLConnection connection = new AceQLConnection(connectionString))
                {
                    await ExecuteExample(connection).ConfigureAwait(false);
                    await connection.CloseAsync();
                }
            }
            else
            {

                AceQLCredential credential = new AceQLCredential(username, password.ToCharArray());
                AceQLConsole.WriteLine("Using AceQLCredential : " + credential);

                // Make sure connection is always closed to close and release server connection into the pool
                using (AceQLConnection connection = new AceQLConnection(connectionString))
                {
                    connection.Credential = credential;
                    await ExecuteExample(connection).ConfigureAwait(false);
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Executes our example using an <see cref="AceQLConnection"/> 
        /// </summary>
        /// <param name="connection"></param>
        public static async Task ExecuteExample(AceQLConnection connection)
        {
            string IN_DIRECTORY = "c:\\test\\";
            string OUT_DIRECTORY = "c:\\test\\out\\";

            await connection.OpenAsync();

            AceQLConsole.WriteLine("host: " + connection.ConnectionString);
            AceQLConsole.WriteLine("aceQLConnection.GetClientVersion(): " + connection.GetClientVersion());
            AceQLConsole.WriteLine("aceQLConnection.GetServerVersion(): " + await connection.GetServerVersionAsync());
            AceQLConsole.WriteLine("AceQL local folder: ");
            AceQLConsole.WriteLine(await AceQLConnection.GetAceQLLocalFolderAsync());

            AceQLTransaction transaction = await connection.BeginTransactionAsync();
            await transaction.CommitAsync();
            transaction.Dispose();

            string sql = "delete from customer";

            AceQLCommand command = new AceQLCommand()
            {
                CommandText = sql,
                Connection = connection
            };
            command.Prepare();

            await command.ExecuteNonQueryAsync();

            for (int i = 0; i < 300; i++)
            {
                sql =
                "insert into customer values (@parm1, @parm2, @parm3, @parm4, @parm5, @parm6, @parm7, @parm8)";

                command = new AceQLCommand(sql, connection);

                int customer_id = i;

                command.Parameters.AddWithValue("@parm1", customer_id);
                command.Parameters.AddWithValue("@parm2", "Sir");
                command.Parameters.AddWithValue("@parm3", "André_" + customer_id);
                command.Parameters.Add(new AceQLParameter("@parm4", "Name_" + customer_id));
                command.Parameters.AddWithValue("@parm5", customer_id + ", road 66");
                command.Parameters.AddWithValue("@parm6", "Town_" + customer_id);
                command.Parameters.AddWithValue("@parm7", customer_id + "1111");
                command.Parameters.Add(new AceQLParameter("@parm8", new AceQLNullValue(AceQLNullType.VARCHAR))); //null value for NULL SQL insert.

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                await command.ExecuteNonQueryAsync(cancellationTokenSource.Token);
            }

            command.Dispose();

            sql = "select * from customer where customer_id > @parm1";
            command = new AceQLCommand(sql, connection);
            command.Parameters.AddWithValue("@parm1", 1);

            // Our dataReader must be disposed to delete underlying downloaded files
            using (AceQLDataReader dataReader = await command.ExecuteReaderAsync())
            {
                //await dataReader.ReadAsync(new CancellationTokenSource().Token)
                while (dataReader.Read())
                {
                    AceQLConsole.WriteLine();
                    int i = 0;
                    AceQLConsole.WriteLine("GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++));
                }
            }

            AceQLConsole.WriteLine("Before delete from orderlog");

            // Do next delete in a transaction because of BLOB
            //transaction = await connection.BeginTransactionAsync();
            //await transaction.CommitAsync();

            sql = "delete from orderlog";
            command = new AceQLCommand(sql, connection);
            await command.ExecuteNonQueryAsync();

            transaction = await connection.BeginTransactionAsync();

            AceQLConsole.WriteLine("Before insert into orderlog");
            try
            {
                for (int j = 1; j < 4; j++)
                {
                    sql =
                    "insert into orderlog values (@parm1, @parm2, @parm3, @parm4, @parm5, @parm6, @parm7, @parm8, @parm9)";

                    command = new AceQLCommand(sql, connection);

                    int customer_id = j;

                    string blobPath = IN_DIRECTORY + "username_koala.jpg";
                    Stream stream = new FileStream(blobPath, FileMode.Open, System.IO.FileAccess.Read);

                    //customer_id integer NOT NULL,
                    //item_id integer NOT NULL,
                    //description character varying(64) NOT NULL,
                    //cost_price numeric,
                    //date_placed date NOT NULL,
                    //date_shipped timestamp without time zone,
                    //jpeg_image oid,
                    //is_delivered numeric,
                    //quantity integer NOT NULL,

                    command.Parameters.AddWithValue("@parm1", customer_id);
                    command.Parameters.AddWithValue("@parm2", customer_id);
                    command.Parameters.AddWithValue("@parm3", "Description_" + customer_id);
                    //command.Parameters.Add(new AceQLParameter("@parm4", new AceQLNullValue(AceQLNullType.DECIMAL))); //null value for NULL SQL insert.
                    command.Parameters.AddWithValue("@parm4", 45.4);
                    command.Parameters.AddWithValue("@parm5", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@parm6", DateTime.UtcNow);
                    // Adds the Blob. (Stream will be closed by AceQLCommand)
                    bool useBlob = true;
                    if (useBlob)
                    {
                        command.Parameters.Add(new AceQLParameter("@parm7", stream));
                    }
                    else
                    {
                        command.Parameters.Add(new AceQLParameter("@parm7", new AceQLNullValue(AceQLNullType.BLOB)));
                    }
                    
                    command.Parameters.AddWithValue("@parm8", 1);
                    command.Parameters.AddWithValue("@parm9", j * 2000);

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                throw exception;
            }

            AceQLConsole.WriteLine("Before select *  from orderlog");

            // Do next selects in a transaction because of BLOB
            transaction = await connection.BeginTransactionAsync();

            sql = "select * from orderlog";
            command = new AceQLCommand(sql, connection);

            using (AceQLDataReader dataReader = await command.ExecuteReaderAsync())
            {
                int k = 0;
                while (dataReader.Read())
                {
                    AceQLConsole.WriteLine();
                    AceQLConsole.WriteLine("Get values using ordinal values:");
                    int i = 0;
                    AceQLConsole.WriteLine("GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++) + "\n"
                        + "GetValue: " + dataReader.GetValue(i++));

                    //customer_id
                    //item_id
                    //description
                    //item_cost
                    //date_placed
                    //date_shipped
                    //jpeg_image
                    //is_delivered
                    //quantity

                    AceQLConsole.WriteLine();
                    AceQLConsole.WriteLine("Get values using column name values:");
                    AceQLConsole.WriteLine("GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("customer_id"))
                        + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("item_id")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("description")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("item_cost")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("date_placed")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("date_shipped")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("jpeg_image")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("is_delivered")) + "\n"
                        + "GetValue: " + dataReader.GetValue(dataReader.GetOrdinal("quantity")));


                    AceQLConsole.WriteLine("==> dataReader.IsDBNull(3): " + dataReader.IsDBNull(3));
                    AceQLConsole.WriteLine("==> dataReader.IsDBNull(4): " + dataReader.IsDBNull(4));

                    // Download Blobs
                    string blobPath = OUT_DIRECTORY + "username_koala_" + k + ".jpg";
                    k++;

                    using (Stream stream = await dataReader.GetStreamAsync(6))
                    {
                        using (var fileStream = File.Create(blobPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }

            await transaction.CommitAsync();
        }

    }
}
