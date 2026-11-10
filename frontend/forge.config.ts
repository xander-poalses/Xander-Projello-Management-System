import type { ForgeConfig } from '@electron-forge/shared-types';
import { MakerSquirrel } from '@electron-forge/maker-squirrel';
import { MakerZIP } from '@electron-forge/maker-zip';
import { MakerDeb } from '@electron-forge/maker-deb';
import { MakerRpm } from '@electron-forge/maker-rpm';
import { AutoUnpackNativesPlugin } from '@electron-forge/plugin-auto-unpack-natives';
import { WebpackPlugin } from '@electron-forge/plugin-webpack';
import { FusesPlugin } from '@electron-forge/plugin-fuses';
import { FuseV1Options, FuseVersion } from '@electron/fuses';

import { mainConfig } from './webpack.main.config';
import { rendererConfig } from './webpack.renderer.config';

// Projello branding for the packaged app and installer
const appName = 'Projello';
const publisher = 'Xander Poalses';
const iconBase = './assets/icons/icon'; // .ico on Windows, .png on Linux

const config: ForgeConfig = {
  packagerConfig: {
    asar: true,
    name: appName,
    executableName: appName,
    icon: iconBase,
    appCopyright: `© ${new Date().getFullYear()} ${publisher}`,
    // Shown in the exe's Properties > Details and in Task Manager
    win32metadata: {
      CompanyName: publisher,
      FileDescription: 'Projello: project management for construction teams',
      ProductName: appName,
      InternalName: appName,
      OriginalFilename: `${appName}.exe`,
    },
  },
  rebuildConfig: {},
  makers: [
    new MakerSquirrel({
      name: appName,
      authors: publisher,
      description: 'Projello: project management for construction teams',
      setupIcon: `${iconBase}.ico`,
      setupExe: `${appName}-Setup.exe`,
    }),
    new MakerZIP({}, ['darwin']),
    new MakerRpm({ options: { icon: `${iconBase}-512.png` } }),
    new MakerDeb({ options: { icon: `${iconBase}-512.png` } }),
  ],
  plugins: [
    new AutoUnpackNativesPlugin({}),
    new WebpackPlugin({
      mainConfig,
      renderer: {
        config: rendererConfig,
        entryPoints: [
          {
            html: './src/index.html',
            js: './src/renderer.tsx',
            name: 'main_window',
            preload: {
              js: './src/preload.ts',
            },
          },
        ],
      },
    }),
    // Fuses are used to enable/disable various Electron functionality
    // at package time, before code signing the application
    new FusesPlugin({
      version: FuseVersion.V1,
      [FuseV1Options.RunAsNode]: false,
      [FuseV1Options.EnableCookieEncryption]: true,
      [FuseV1Options.EnableNodeOptionsEnvironmentVariable]: false,
      [FuseV1Options.EnableNodeCliInspectArguments]: false,
      [FuseV1Options.EnableEmbeddedAsarIntegrityValidation]: true,
      [FuseV1Options.OnlyLoadAppFromAsar]: true,
    }),
  ],
};

export default config;
