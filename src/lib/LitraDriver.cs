using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using HidSharp;


public enum DeviceType
{
    LitraGlow,
    LitraBeam,
    LitraBeamLX,
}

public interface IDevice
{
    HidDevice HidDevice { get; }
    DeviceType Type { get; }
    string SerialNumber { get; }
}

public class Device : IDevice
{
    public HidDevice HidDevice { get; }
    public DeviceType Type { get; }
    public string SerialNumber { get; }

    public Device(HidDevice hidDevice, DeviceType type, string serialNumber)
    {
        HidDevice = hidDevice;
        Type = type;
        SerialNumber = serialNumber;
    }
}

public class LitraDriver
{
    private const int VendorId = 0x046d;
    private static readonly int[] ProductIds = { 0xc900, 0xc901, 0xb901, 0xc903 };

    private static readonly Dictionary<DeviceType, int> MinimumBrightnessInLumenByDeviceType = new()
        {
            { DeviceType.LitraGlow, 20 },
            { DeviceType.LitraBeam, 30 },
            { DeviceType.LitraBeamLX, 30 },
        };

    private static readonly Dictionary<DeviceType, int> MaximumBrightnessInLumenByDeviceType = new()
        {
            { DeviceType.LitraGlow, 250 },
            { DeviceType.LitraBeam, 400 },
            { DeviceType.LitraBeamLX, 400 },
        };

    private static readonly List<int> MultiplesOf100Between2700And6500 =
        GenerateMultiplesWithinRange(100, 2700, 6500);

    private static readonly Dictionary<DeviceType, List<int>> AllowedTemperaturesInKelvinByDeviceType = new()
        {
            { DeviceType.LitraGlow, MultiplesOf100Between2700And6500 },
            { DeviceType.LitraBeam, MultiplesOf100Between2700And6500 },
            { DeviceType.LitraBeamLX, MultiplesOf100Between2700And6500 },
        };

    private static bool IsLitraDevice(HidDevice device)
    {
        return device.VendorID == VendorId &&
               ProductIds.Contains(device.ProductID) &&
               device.DevicePath.Contains("col02", StringComparison.OrdinalIgnoreCase);
    }

    private static Device HidDeviceToDevice(HidDevice hidDevice)
    {
        var type = GetDeviceTypeByProductId(hidDevice.ProductID);
        var serial = ExtractSerialNumberFromDevice(hidDevice);
        return new Device(hidDevice, type, serial);
    }

    public static Device? FindDevice()
    {
        var devices = DeviceList.Local.GetHidDevices().ToList();
        var match = devices.FirstOrDefault(IsLitraDevice);
        return match != null ? HidDeviceToDevice(match) : null;
    }

    public static List<Device> FindDevices()
    {
        return DeviceList.Local.GetHidDevices()
            .Where(IsLitraDevice)
            .Select(HidDeviceToDevice)
            .Distinct()
            .ToList();
    }

    // -------------------------------
    // SERIAL NUMBER EXTRACTION (Faked by hashing the device path since HidSharp doesn't support HidD_GetSerialNumberString)
    // -------------------------------

    private static string ExtractSerialNumberFromDevice(HidDevice device)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(device.DevicePath))
        )[..8];
    }


    // -------------------------------
    // HID COMMANDS
    // -------------------------------

    private static void Write(Device device, byte[] data)
    {
        using var stream = device.HidDevice.Open();
        stream.Write(data);
    }

    private static byte[] Read(Device device)
    {
        using var stream = device.HidDevice.Open();
        var buffer = new byte[64];
        stream.Read(buffer);
        return buffer;
    }

    public static void TurnOn(Device device)
    {
        Write(device, GenerateTurnOnBytes(device));
    }

    public static void TurnOff(Device device)
    {
        Write(device, GenerateTurnOffBytes(device));
    }

    public static void Toggle(Device device)
    {
        if (IsOn(device))
            TurnOff(device);
        else
            TurnOn(device);
    }

    public static bool IsOn(Device device)
    {
        Write(device, GenerateIsOnBytes(device));
        var data = Read(device);
        return data[4] == 1;
    }

    public static void SetTemperatureInKelvin(Device device, int kelvin)
    {
        var allowed = AllowedTemperaturesInKelvinByDeviceType[device.Type];
        if (!allowed.Contains(kelvin))
            throw new ArgumentException("Invalid temperature");

        Write(device, GenerateSetTemperatureInKelvinBytes(device, kelvin));
    }

    public static int GetTemperatureInKelvin(Device device)
    {
        Write(device, GenerateGetTemperatureInKelvinBytes(device));
        var data = Read(device);
        return data[4] * 256 + data[5];
    }

    public static void SetBrightnessInLumen(Device device, int lumen)
    {
        var min = MinimumBrightnessInLumenByDeviceType[device.Type];
        var max = MaximumBrightnessInLumenByDeviceType[device.Type];

        if (lumen < min || lumen > max)
            throw new ArgumentException("Invalid brightness");

        Write(device, GenerateSetBrightnessInLumenBytes(device, lumen));
    }

    public static int GetBrightnessInLumen(Device device)
    {
        Write(device, GenerateGetBrightnessInLumenBytes(device));
        var data = Read(device);
        return data[4] * 256 + data[5];
    }

    // -------------------------------
    // COMMAND GENERATION
    // -------------------------------

    private static byte[] GenerateTurnOnBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new byte[] { 0x11, 0xff, 0x06, 0x1c, 0x01 }, 20, 0x00)
            : PadRight(new byte[] { 0x11, 0xff, 0x04, 0x1c, 0x01 }, 20, 0x00);
    }

    private static byte[] GenerateTurnOffBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new byte[] { 0x11, 0xff, 0x06, 0x1c, 0x00 }, 20, 0x00)
            : PadRight(new byte[] { 0x11, 0xff, 0x04, 0x1c, 0x00 }, 20, 0x00);
    }

    private static byte[] GenerateIsOnBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new byte[] { 0x11, 0xff, 0x06, 0x01 }, 20, 0x00)
            : PadRight(new byte[] { 0x11, 0xff, 0x04, 0x01 }, 20, 0x00);
    }

    private static byte[] GenerateSetTemperatureInKelvinBytes(Device device, int kelvin)
    {
        var baseBytes = new List<byte> { 0x11, 0xff };

        if (device.Type == DeviceType.LitraBeamLX)
            baseBytes.AddRange(new byte[] { 0x06, 0x9c });
        else
            baseBytes.AddRange(new byte[] { 0x04, 0x9c });

        baseBytes.AddRange(IntegerToBytes(kelvin));
        return PadRight(baseBytes.ToArray(), 20, 0x00);
    }

    private static byte[] GenerateGetTemperatureInKelvinBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new byte[] { 0x11, 0xff, 0x06, 0x81 }, 20, 0x00)
            : PadRight(new byte[] { 0x11, 0xff, 0x04, 0x81 }, 20, 0x00);
    }

    private static byte[] GenerateSetBrightnessInLumenBytes(Device device, int lumen)
    {
        var baseBytes = new List<byte> { 0x11, 0xff };

        if (device.Type == DeviceType.LitraBeamLX)
            baseBytes.AddRange(new byte[] { 0x06, 0x4c });
        else
            baseBytes.AddRange(new byte[] { 0x04, 0x4c });

        baseBytes.AddRange(IntegerToBytes(lumen));
        return PadRight(baseBytes.ToArray(), 20, 0x00);
    }

    private static byte[] GenerateGetBrightnessInLumenBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new byte[] { 0x11, 0xff, 0x06, 0x31 }, 20, 0x00)
            : PadRight(new byte[] { 0x11, 0xff, 0x04, 0x31 }, 20, 0x00);
    }

    // -------------------------------
    // HELPERS
    // -------------------------------

    private static DeviceType GetDeviceTypeByProductId(int productId)
    {
        return productId switch
        {
            0xc900 => DeviceType.LitraGlow,
            0xc901 => DeviceType.LitraBeam,
            0xb901 => DeviceType.LitraBeamLX,
            0xc903 => DeviceType.LitraBeamLX,
            _ => throw new ArgumentException($"Unknown product ID: {productId}")
        };
    }

    private static List<int> GenerateMultiplesWithinRange(int multiple, int min, int max)
    {
        var result = new List<int>();
        for (int i = min; i <= max; i += multiple)
            result.Add(i);
        return result;
    }

    private static byte[] PadRight(byte[] bytes, int length, byte paddingByte)
    {
        if (bytes.Length >= length)
            return bytes;

        var padded = new byte[length];
        Array.Copy(bytes, padded, bytes.Length);
        for (int i = bytes.Length; i < length; i++)
            padded[i] = paddingByte;

        return padded;
    }

    private static byte[] IntegerToBytes(int value)
    {
        return new byte[]
        {
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF)
        };
    }

    // -------------------------------
    // WINDOWS API
    // -------------------------------

    private const uint FileAccessRead = 0x80000000;
    private const uint FileAccessWrite = 0x40000000;

    [Flags]
    private enum FileAccess : uint
    {
        Read = FileAccessRead,
        Write = FileAccessWrite
    }

    [Flags]
    private enum FileShare : uint
    {
        Read = 1,
        Write = 2
    }

    private enum CreationDisposition : uint
    {
        OpenExisting = 3
    }

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        FileAccess dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        CreationDisposition dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );
}
