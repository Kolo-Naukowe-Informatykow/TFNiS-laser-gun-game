using Godot;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.Versioning;
using System.Text;

public sealed class LightGunSerialPort : IDisposable
{
    private static readonly TimeSpan ReconnectCooldown = TimeSpan.FromSeconds(2);

    private readonly string _targetVid;
    private readonly int _baudRate;
    private SerialPort _serialPort;
    private DateTime _nextReconnectAttemptUtc = DateTime.MinValue;
    private bool _noPortWarningShown;

    public LightGunSerialPort(string targetVid, int baudRate)
    {
        _targetVid = NormalizeVid(targetVid);
        _baudRate = baudRate;
    }

    public bool IsConnected => _serialPort?.IsOpen == true;

    public bool Connect()
    {
        if (IsConnected)
        {
            return true;
        }

        if (DateTime.UtcNow < _nextReconnectAttemptUtc)
        {
            return false;
        }

        _nextReconnectAttemptUtc = DateTime.UtcNow + ReconnectCooldown;

        var matchingPorts = GetPortsForVid(_targetVid);
        foreach (var portName in matchingPorts)
        {
            try
            {
                _serialPort = new SerialPort(portName, _baudRate)
                {
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 100,
                    WriteTimeout = 100
                };
                _serialPort.Open();
                _noPortWarningShown = false;
                GD.Print($"[LightGun] Connected on {portName} for VID {_targetVid}.");
                return true;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[LightGun] Could not open {portName}: {ex.Message}");
                _serialPort?.Dispose();
                _serialPort = null;
            }
        }

        if (!_noPortWarningShown)
        {
            GD.PushWarning($"[LightGun] No serial port found for VID {_targetVid}. Reconnect attempts are throttled.");
            _noPortWarningShown = true;
        }

        return false;
    }

    public void SendAscii(string payload)
    {
        if (!EnsureConnected())
        {
            return;
        }

        try
        {
            _serialPort.Write(payload);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[LightGun] Failed to send ASCII payload: {ex.Message}");
        }
    }

    public void SendNotation(string payload)
    {
        if (!EnsureConnected())
        {
            return;
        }

        try
        {
            var bytes = ParseHexNotation(payload);
            _serialPort.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[LightGun] Failed to send notation payload: {ex.Message}");
        }
    }

    public void Close()
    {
        if (_serialPort == null)
        {
            return;
        }

        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[LightGun] Error while closing serial port: {ex.Message}");
        }
        finally
        {
            _serialPort.Dispose();
            _serialPort = null;
        }
    }

    public void Dispose()
    {
        Close();
    }

    private bool EnsureConnected()
    {
        return IsConnected || Connect();
    }

    private static string NormalizeVid(string vid)
    {
        if (string.IsNullOrWhiteSpace(vid))
        {
            return "0000";
        }

        var normalized = vid.Trim().ToUpperInvariant();
        return normalized.StartsWith("0X") ? normalized[2..] : normalized;
    }

    private static string[] GetPortsForVid(string vid)
    {
        var connectedPorts = new HashSet<string>(SerialPort.GetPortNames(), StringComparer.OrdinalIgnoreCase);

        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        return GetPortsForVidWindows(vid, connectedPorts);
    }

    [SupportedOSPlatform("windows")]
    private static string[] GetPortsForVidWindows(string vid, HashSet<string> connectedPorts)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var usbRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
        if (usbRoot == null)
        {
            return [];
        }

        foreach (var deviceKeyName in usbRoot.GetSubKeyNames())
        {
            if (!deviceKeyName.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var deviceKey = usbRoot.OpenSubKey(deviceKeyName);
            if (deviceKey == null)
            {
                continue;
            }

            foreach (var instanceName in deviceKey.GetSubKeyNames())
            {
                using var instanceKey = deviceKey.OpenSubKey(instanceName);
                using var deviceParams = instanceKey?.OpenSubKey("Device Parameters");
                var portName = deviceParams?.GetValue("PortName") as string;
                if (!string.IsNullOrWhiteSpace(portName) && connectedPorts.Contains(portName))
                {
                    result.Add(portName);
                }
            }
        }

        var ports = new string[result.Count];
        result.CopyTo(ports);
        return ports;
    }

    private static byte[] ParseHexNotation(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return [];
        }

        var bytes = new List<byte>(command.Length);

        for (var i = 0; i < command.Length; i++)
        {
            if (command[i] == '\\' && i + 3 < command.Length && (command[i + 1] == 'x' || command[i + 1] == 'X'))
            {
                if (TryParseHexByte(command, i + 2, out var hexByte))
                {
                    bytes.Add(hexByte);
                    i += 3;
                    continue;
                }
            }

            if ((command[i] == 'x' || command[i] == 'X') && i + 2 < command.Length)
            {
                if (TryParseHexByte(command, i + 1, out var hexByte))
                {
                    bytes.Add(hexByte);
                    i += 2;
                    continue;
                }
            }

            bytes.Add((byte)command[i]);
        }

        return [.. bytes];
    }

    private static bool TryParseHexByte(string source, int offset, out byte value)
    {
        value = 0;

        if (offset + 1 >= source.Length)
        {
            return false;
        }

        var hex = source.Substring(offset, 2);
        return byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out value);
    }
}
