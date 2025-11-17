// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)
const prodUrl = 'https://fable-hub.github.io'
const organizationName = 'fable-hub'
const projectName = 'fable-electron'
const projectUrl = 'https://github.com/fable-hub/fable-electron'

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Fable.Electron',
  tagline: 'Full suite of bindings and helpers for creating ElectronJS apps entirely in F# with Fable!',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: prodUrl,
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',
    trailingSlash: false,

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: organizationName, // Usually your GitHub org/user name.
  projectName: projectName, // Usually your repo name.

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/fable-hub/fable-electron',
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ['rss', 'atom'],
            xslt: true,
          },
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/fable-hub/fable-electron',
          // Useful options to enforce blogging best practices
          onInlineTags: 'warn',
          onInlineAuthors: 'warn',
          onUntruncatedBlogPosts: 'warn',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],
    themes: [ '@docusaurus/theme-mermaid' ],
    markdown: {
      mermaid: true,
    },

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/docusaurus-social-card.jpg',
      colorMode: {
        respectPrefersColorScheme: true,
      },
      navbar: {
          hideOnScroll: true,
        title: 'Fable.Electron',
        logo: {
          alt: 'Fable.Electron Logo',
          src: 'img/feleico.png',
            srcDark: 'img/feleico-dark.png',
        },
        items: [
            {
                type: 'docSidebar',
                sidebarId: 'guideSidebar',
                position: 'left',
                label: 'Guide',
            },
            {
                type: 'docSidebar',
                sidebarId: 'devSidebar',
                position: 'left',
                label: 'Dev',
            },
            {to: '/blog', label: 'Blog', position: 'left'},
            {
                label: 'API',
                href: 'https://shayanhabibi.github.io/fable-electron-docs-api/reference/',
                position: 'left',
            },
          {
            href: projectUrl,
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
          logo: {
            alt: 'Fable.Electron Logo',
              src: 'img/feleico-dark.png',
              width: '64px',
              height: '64px',
          },
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Guide',
                to: '/docs/guide/intro',
              },
                {
                    label: 'Dev',
                    to: '/docs/dev/intro',
                },
                {
                    label: 'API',
                    href: 'https://shayanhabibi.github.io/fable-electron-docs-api/reference/',
                },
            ],
          },
          {
            title: 'Community',
            items: [
              // {
              //   label: 'Discord',
              //   href: 'https://discordapp.com/invite/docusaurus',
              // },
                {
                    label: 'GitHub',
                    href: projectUrl,
                }
            ],
          },
            {
                title: 'Related',
                items: [
                    {
                        label: 'Fable',
                        href: 'https://fable.io',
                    },
                    {
                        label: 'Electron',
                        href: 'https://electron.io',
                    },
                ]
            },
          {
            title: 'More',
            items: [
              {
                label: 'Blog',
                to: '/blog',
              },
            ],
          },
        ],
        copyright: `Copyright © ${new Date().getFullYear()} Shayan Habibi & Fable.Electron contributors`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
          additionalLanguages: [ 'fsharp' ],
          defaultLanguage: 'fsharp',
      },
    }),
};

export default config;
