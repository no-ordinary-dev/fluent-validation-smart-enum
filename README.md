# FluentValidation.SmartEnum

[![NoOrdinaryDev.FluentValidation.SmartEnum NuGet Package](https://img.shields.io/nuget/v/NoOrdinaryDev.FluentValidation.SmartEnum.svg)](https://www.nuget.org/packages/NoOrdinaryDev.FluentValidation.SmartEnum/) [![NoOrdinaryDev.FluentValidation.SmartEnum NuGet Package Downloads](https://img.shields.io/nuget/dt/NoOrdinaryDev.FluentValidation.SmartEnum)](https://www.nuget.org/packages/NoOrdinaryDev.FluentValidation.SmartEnum) [![GitHub Actions Status](https://github.com/no-ordinary-dev/fluent-validation-smart-enum/workflows/Build/badge.svg?branch=main)](https://github.com/no-ordinary-dev/fluent-validation-smart-enum/actions)

[![GitHub Actions Build History](https://buildstats.info/github/chart/no-ordinary-dev/fluent-validation-smart-enum?branch=main&includeBuildsFromPullRequest=false)](https://github.com/no-ordinary-dev/fluent-validation-smart-enum/actions)

FluentValidation extensions for validating [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) values.

## Why?

`FluentValidation` has `IsInEnum()` for regular C# enums, but SmartEnum values are not regular enums. This package adds a reusable validator that checks whether a value matches a SmartEnum member.

## Installation

```bash
dotnet add package FluentValidation.SmartEnum