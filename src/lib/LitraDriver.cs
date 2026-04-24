#nullable enable
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
    String SerialNumber { get; }
}

public class Device : IDevice
{
    public HidDevice HidDevice { get; }
    public DeviceType Type { get; }
    public String SerialNumber { get; }

    public Device(HidDevice hidDevice, DeviceType type, String serialNumber)
    {
        HidDevice = hidDevice;
        Type = type;
        SerialNumber = serialNumber;
    }
}

public class LitraDriver
{
    private const Int32 VendorId = 0x046d;
    private static readonly Int32[] ProductIds = { 0xc900, 0xc901, 0xb901, 0xc903 };

    private static readonly Dictionary<DeviceType, Int32> MinimumBrightnessInLumenByDeviceType = new()
        {
            { DeviceType.LitraGlow, 20 },
            { DeviceType.LitraBeam, 30 },
            { DeviceType.LitraBeamLX, 30 },
        };

    private static readonly Dictionary<DeviceType, Int32> MaximumBrightnessInLumenByDeviceType = new()
        {
            { DeviceType.LitraGlow, 250 },
            { DeviceType.LitraBeam, 400 },
            { DeviceType.LitraBeamLX, 400 },
        };

    private static readonly List<Int32> MultiplesOf100Between2700And6500 =
        GenerateMultiplesWithinRange(100, 2700, 6500);

    private static readonly Dictionary<DeviceType, List<Int32>> AllowedTemperaturesInKelvinByDeviceType = new()
        {
            { DeviceType.LitraGlow, MultiplesOf100Between2700And6500 },
            { DeviceType.LitraBeam, MultiplesOf100Between2700And6500 },
            { DeviceType.LitraBeamLX, MultiplesOf100Between2700And6500 },
        };

    private static Boolean IsLitraDevice(HidDevice device)
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

    private static String ExtractSerialNumberFromDevice(HidDevice device)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(device.DevicePath))
        )[..8];
    }


    // -------------------------------
    // HID COMMANDS
    // -------------------------------

    private static void Write(Device device, Byte[] data)
    {
        using var stream = device.HidDevice.Open();
        stream.Write(data);
    }

    private static Byte[] Read(Device device)
    {
        using var stream = device.HidDevice.Open();
        var buffer = new Byte[64];
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
        {
            TurnOff(device);
        }
        else
        {
            TurnOn(device);
        }

    }

    public static Boolean IsOn(Device device)
    {
        Write(device, GenerateIsOnBytes(device));
        var data = Read(device);
        return data[4] == 1;
    }

    public static void SetTemperatureInKelvin(Device device, Int32 kelvin)
    {
        var allowed = AllowedTemperaturesInKelvinByDeviceType[device.Type];
        if (!allowed.Contains(kelvin))
        {

            throw new ArgumentException("Invalid temperature");
        }


        Write(device, GenerateSetTemperatureInKelvinBytes(device, kelvin));
    }

    public static Int32 GetTemperatureInKelvin(Device device)
    {
        Write(device, GenerateGetTemperatureInKelvinBytes(device));
        var data = Read(device);
        return data[4] * 256 + data[5];
    }

    public static Int32 GetTemperatureInPercent(Device device)
    {
        var current = GetTemperatureInKelvin(device);
        var allowed = AllowedTemperaturesInKelvinByDeviceType[device.Type];
        var min = allowed.Min();
        var max = allowed.Max();
        return (current - min) * 100 / (max - min);
    }

    public static void SetTemperatureInPercent(Device device, Int32 percent)
    {
        var allowed = AllowedTemperaturesInKelvinByDeviceType[device.Type];
        var min = allowed.Min();
        var max = allowed.Max();
        var kelvin = (percent * (max - min) / 100) + min;
        var nearestKelvin = allowed.OrderBy(t => Math.Abs(t - kelvin)).First();
        SetTemperatureInKelvin(device, nearestKelvin);
    }

    public static void SetBrightnessInLumen(Device device, Int32 lumen)
    {
        var min = MinimumBrightnessInLumenByDeviceType[device.Type];
        var max = MaximumBrightnessInLumenByDeviceType[device.Type];

        if (lumen < min || lumen > max)
        {

            throw new ArgumentException("Invalid brightness");
        }


        Write(device, GenerateSetBrightnessInLumenBytes(device, lumen));
    }

    public static Int32 GetBrightnessInLumen(Device device)
    {
        Write(device, GenerateGetBrightnessInLumenBytes(device));
        var data = Read(device);
        return data[4] * 256 + data[5];
    }

    public static Int32 GetMinimumBrightnessInLumen(Device device) => MinimumBrightnessInLumenByDeviceType[device.Type];

    public static Int32 GetMaximumBrightnessInLumen(Device device) => MaximumBrightnessInLumenByDeviceType[device.Type];

    public static Int32 GetBrightnessPercent(Device device)
    {
        var current = GetBrightnessInLumen(device);
        var min = GetMinimumBrightnessInLumen(device);
        var max = GetMaximumBrightnessInLumen(device);
        return (current - min) * 100 / (max - min);
    }

    public static void SetBrightnessPercent(Device device, Int32 percent)
    {
        var min = GetMinimumBrightnessInLumen(device);
        var max = GetMaximumBrightnessInLumen(device);
        var lumen = (percent * (max - min) / 100) + min;
        SetBrightnessInLumen(device, lumen);
    }

    // -------------------------------
    // COMMAND GENERATION
    // -------------------------------

    private static Byte[] GenerateTurnOnBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new Byte[] { 0x11, 0xff, 0x06, 0x1c, 0x01 }, 20, 0x00)
            : PadRight(new Byte[] { 0x11, 0xff, 0x04, 0x1c, 0x01 }, 20, 0x00);
    }

    private static Byte[] GenerateTurnOffBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new Byte[] { 0x11, 0xff, 0x06, 0x1c, 0x00 }, 20, 0x00)
            : PadRight(new Byte[] { 0x11, 0xff, 0x04, 0x1c, 0x00 }, 20, 0x00);
    }

    private static Byte[] GenerateIsOnBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new Byte[] { 0x11, 0xff, 0x06, 0x01 }, 20, 0x00)
            : PadRight(new Byte[] { 0x11, 0xff, 0x04, 0x01 }, 20, 0x00);
    }

    private static Byte[] GenerateSetTemperatureInKelvinBytes(Device device, Int32 kelvin)
    {
        var baseBytes = new List<Byte> { 0x11, 0xff };

        if (device.Type == DeviceType.LitraBeamLX)
        {
            baseBytes.AddRange(new Byte[] { 0x06, 0x9c });
        }
        else
        {
            baseBytes.AddRange(new Byte[] { 0x04, 0x9c });
        }


        baseBytes.AddRange(IntegerToBytes(kelvin));
        return PadRight(baseBytes.ToArray(), 20, 0x00);
    }

    private static Byte[] GenerateGetTemperatureInKelvinBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new Byte[] { 0x11, 0xff, 0x06, 0x81 }, 20, 0x00)
            : PadRight(new Byte[] { 0x11, 0xff, 0x04, 0x81 }, 20, 0x00);
    }

    private static Byte[] GenerateSetBrightnessInLumenBytes(Device device, Int32 lumen)
    {
        var baseBytes = new List<Byte> { 0x11, 0xff };

        if (device.Type == DeviceType.LitraBeamLX)
        {
            baseBytes.AddRange(new Byte[] { 0x06, 0x4c });
        }
        else
        {
            baseBytes.AddRange(new Byte[] { 0x04, 0x4c });
        }


        baseBytes.AddRange(IntegerToBytes(lumen));
        return PadRight(baseBytes.ToArray(), 20, 0x00);
    }

    private static Byte[] GenerateGetBrightnessInLumenBytes(Device device)
    {
        return device.Type == DeviceType.LitraBeamLX
            ? PadRight(new Byte[] { 0x11, 0xff, 0x06, 0x31 }, 20, 0x00)
            : PadRight(new Byte[] { 0x11, 0xff, 0x04, 0x31 }, 20, 0x00);
    }

    // -------------------------------
    // HELPERS
    // -------------------------------

    private static DeviceType GetDeviceTypeByProductId(Int32 productId)
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

    private static List<Int32> GenerateMultiplesWithinRange(Int32 multiple, Int32 min, Int32 max)
    {
        var result = new List<Int32>();
        for (var i = min; i <= max; i += multiple)
        {
            result.Add(i);
        }


        return result;
    }

    private static Byte[] PadRight(Byte[] bytes, Int32 length, Byte paddingByte)
    {
        if (bytes.Length >= length)
        {

            return bytes;
        }


        var padded = new Byte[length];
        Array.Copy(bytes, padded, bytes.Length);
        for (var i = bytes.Length; i < length; i++)
        {
            padded[i] = paddingByte;
        }


        return padded;
    }

    private static Byte[] IntegerToBytes(Int32 value)
    {
        return new Byte[]
        {
                (Byte)((value >> 8) & 0xFF),
                (Byte)(value & 0xFF)
        };
    }

    // -------------------------------
    // WINDOWS API
    // -------------------------------

    private const UInt32 FileAccessRead = 0x80000000;
    private const UInt32 FileAccessWrite = 0x40000000;

    [Flags]
    private enum FileAccess : UInt32
    {
        Read = FileAccessRead,
        Write = FileAccessWrite
    }

    [Flags]
    private enum FileShare : UInt32
    {
        Read = 1,
        Write = 2
    }

    private enum CreationDisposition : UInt32
    {
        OpenExisting = 3
    }

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        String lpFileName,
        FileAccess dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        CreationDisposition dwCreationDisposition,
        UInt32 dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );
}
