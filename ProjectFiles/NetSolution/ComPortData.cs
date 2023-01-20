#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.UI;
using FTOptix.SerialPort;
using FTOptix.CommunicationDriver;
using System.Threading;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
#endregion

public class ComPortData : BaseNetLogic
{
    private SerialPort serialPort;
    private PeriodicTask periodicTask;
    public override void Start()
    {
        serialPort = (SerialPort)Project.Current.Get("CommDrivers/SerialPort1");
        periodicTask = new PeriodicTask(Read, 300, Owner);
        periodicTask.Start();
    }
    public override void Stop()
    {
        periodicTask.Dispose();
        serialPort.Stop();
    }
    [ExportMethod]
    public void RestartComm()
    {
        Stop();
        Thread.Sleep(1000);
        Start();
    }

    private void Read()
    {
        try
        {
            ReadImpl();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to read from Modbus: " + ex);
        }
    }
    private void ReadImpl()
    {
        serialPort.WriteBytes(Serialize());
        var result = serialPort.ReadBytes(3);
        if ((result[1] & 0x80) == 0)
        {
            result = serialPort.ReadBytes((uint)(result[2] + 2));
            Log.Info(Deserialize(result).ToString());
        }
        else
        {
            Log.Error("Failed to read from Modbus");
        }
    }
    private byte[] Serialize()
    {
        var buffer = new byte[]
        {
            0x01, // UnitId
            0x03, // Function code
            0x00, // Starting address
            0x00,
            0x00, // Quantity Of Registers
            0x01,
            0x84, // CRC
            0x0a
        };
        return buffer;
    }
    private ushort Deserialize(byte[] buffer)
    {
        var first = (ushort)buffer[1];
        var second = (ushort)(buffer[0] << 8);
        return (ushort)(first | second);
    }
}
