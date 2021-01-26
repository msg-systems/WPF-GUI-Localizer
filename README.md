# WPF-GUI-Localizer

[![Version](https://img.shields.io/nuget/v/GUILocalizer)](https://www.nuget.org/packages/GUILocalizer)

## Introduction

This library tackles a problem many software projects face: Translations are done based on tables (Excel or similar) outside of the actual application and outside of the context of the actual application. This leads to inconsistent translations - how can a translator translate a word with multiple possible translations without knowing the context in which it is used?

The library offers a functionality to define and edit translations of GUI-Elements at runtime as presented in the application using a pop-up window, as shown below.

![Dialog Pop-up Window](Docs/dialog.PNG)

It comes along with the ability to localize WPF applications using either human-readable Excel files or traditional Resources (.resx) files.

## Install

The WPF-GUI-Localizer library can be loaded via [NuGet](https://www.nuget.org/packages/GUILocalizer).

As dependencies, both the .NET Framework version 4.7.2 or higher as well as Excel need to be installed.

## Trying out the library

You can run an demo app utilizing this library by loading it from the 'Examples' folder of this repository (no IDE required).

Alternatively you can open the WPF-GUI-Localizer solution in an IDE and run or debug the Example_Excel and Example_Resources projects.

Or read more about how to get set up with your own application [here](Docs/documentation.md#setup) or check out the quick-start-checklist for either [Excel based localization](Docs/documentation.md#excelquickstart) or [Resource file based localization](Docs/documentation.md#resourcequickstart).

## Documentation

The full documentation can be found [here](Docs/documentation.md)

## License

The WPF-GUI-Localizer library is licensed under the MIT License. See the file [LICENSE](LICENSE) for more details.

-----

Authors: [Martin Fabian Thomas, msg systems ag](mailto:martin.thomas@msg.group),
[Fabian Lewalder, msg systems ag](mailto:fabian.lewalder@msg.group)
