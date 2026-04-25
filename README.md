
# Litra Glow Plugin for Logitech Creative Console

A plugin for the Logitech Creative Console that allows you to control the Litra Glow light.

This project uses and builds upon the excellent [`litra`](https://github.com/timrogers/litra) package by [timrogers](https://github.com/timrogers), converted to C# by myself using GitHub Copilot.

## Supported Devices

- Litra Glow
- Litra Beam (untested)
- Litra Beam LX (untested)

The plugin was only tested with the **Litra Glow** as it is the only one I have.

If you have either of the Litra Beams, feel free to open a pull request with any necessary changes to support it.

## Features

- Toggle lights on/off
- Select which light(s) to control for each button/dial
- Adjust brightness (using dials or buttons)
- Adjust color temperature (using dials or buttons)
- Set predetermined brightness and color temperature levels

## Known Issues

- Sometimes when using dials, it'll jump directly either 0% or 100%.

## Planned Features

- [ ] Increase/decrease brightness and color temperature by a relative amount when pressing the buttons.

## Credits

- [timrogers/litra](https://github.com/timrogers/litra) — JavaScript control library for Litra lights

## Special Thanks

- **Paul Fitzsimons** from **Logitech** for providing me with a Creative Console device for development and testing.

## License

MIT License. See [LICENSE](LICENSE) file.
