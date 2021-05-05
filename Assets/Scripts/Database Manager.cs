using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mono.Data.Sqlite;
using System.Data;
using System.Linq;

public class DatabaseManager{

    // Variable for the containing object to hold a reference to this script
    private static DatabaseManager s_Instance;
    public static DatabaseManager Instance
    {
        get
        {
            return s_Instance;
        }
    }

    public static DatabaseManager Create()
    {
        try
        {
            // If there is no instance of this script
            if (null == s_Instance)
            {
                s_Instance = Activator.CreateInstance(typeof(DatabaseManager), true) as DatabaseManager;
            }
            // else do no create another DatabaseManager
            else
            {
                string exceptionMessage = System.String.Format("Instance of {0} already exists", typeof(DatabaseManager).ToString());
                throw new Exception(exceptionMessage);
            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }

        // Return the instance
        return s_Instance;
    }

    // Clear the reference to this script
    public static void Destroy()
    {
        if (s_Instance != null)
        {
            s_Instance = null;
        }
    }

    public override string ToString()
    {
        return "Base Manager Class";
    }

    // Create a SQlite connection
    private Dictionary<string, SqliteConnection> _openConnections = new Dictionary<string, SqliteConnection>();
    private IDbConnection _liveConnection = null;

    // Load a database file
    public bool LoadDatabase(string a_dbName)
    {
        // Get the url for the database file
        string loadDb = "URI=file:" + Application.dataPath + "/StreamingAssets/" + a_dbName;

        // Open the connection
        SqliteConnection connection = new SqliteConnection(loadDb);

        // If the connection was opened successfully
        if (connection != null)
        {
            // Get the database file
            _openConnections.Add(a_dbName, connection);

            return true;
        }
        return false;
    }

    // Opens the connection to a file
    public bool OpenConnection(string a_dbName)
    {
        if (_openConnections.ContainsKey(a_dbName))
        {
            SqliteConnection connection = _openConnections[a_dbName];
            _liveConnection = connection as IDbConnection;
            _liveConnection.Open();
        }
        return false;
    }

    // Closes the connection
    public void CloseConnection()
    {
        if (_liveConnection != null)
        {
            _liveConnection.Close();
            _liveConnection = null;
        }
    }

    // Executes a SQL query on an open connection to a database
    public DataTable ExecuteQuery(string a_query)
    {
        // If there is an active connection
        if (_liveConnection != null)
        {
            // Create a command
            IDbCommand dbcmc = _liveConnection.CreateCommand();
            // Set the command text
            dbcmc.CommandText = a_query;
            // Execute the command
            IDataReader reader = dbcmc.ExecuteReader();

            // Seperate the returning data into strings
            List<string> columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            // Create a data table with the previous data
            DataTable dataTable = new DataTable(columns);

            // Transfer the data to the dataTable
            while (reader.Read())
            {
                object[] rowData = new object[reader.FieldCount];
                reader.GetValues(rowData);
                dataTable.AddRow(rowData);
            }

            // Close the connection
            reader.Close();
            // Releases the resources used by the IDataReader
            dbcmc.Dispose();

            // Returns the data from the query
            return dataTable;
        }

        // or returns null
        return null;
    }

}
