using Dalamud.Game.Command;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XIVAutoAttack.Actions;
using XIVAutoAttack.Combos;
using XIVAutoAttack.Combos.CustomCombo;
using XIVAutoAttack.Configuration;
using XIVAutoAttack.Helpers;
using XIVAutoAttack.SigReplacers;
using XIVAutoAttack.Updaters;
using XIVAutoAttack.Windows;

namespace XIVAutoAttack;

public sealed class XIVAutoAttackPlugin : IDalamudPlugin, IDisposable
{
    private const string _command = "/pattack";

    internal const string _autoCommand = "/aauto";

    private readonly WindowSystem windowSystem;

    private static ConfigWindow configWindow;
    //private readonly SystemSound sound;
    public string Name => "XIV Auto Attack";

    internal static readonly ClassJob[] AllJobs = Service.DataManager.GetExcelSheet<ClassJob>().ToArray();

    public XIVAutoAttackPlugin(DalamudPluginInterface pluginInterface, CommandManager commandManager)
    {
        commandManager.AddHandler(_command, new CommandInfo(OnCommand)
        {
            HelpMessage = "打开一个设置各个职业是否启用自动攻击的窗口",
            ShowInHelp = true,
        });

        commandManager.AddHandler(_autoCommand, new CommandInfo(AttackObject)
        {
            HelpMessage = "设置攻击的模式",
            ShowInHelp = true,
        });

        pluginInterface.Create<Service>();
        Service.Configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        Service.Address = new PluginAddressResolver();
        Service.Address.Setup();

        Service.IconReplacer = new IconReplacer();

        configWindow = new ConfigWindow();
        windowSystem = new WindowSystem(Name);
        windowSystem.AddWindow(configWindow);

        Service.Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Service.Interface.UiBuilder.Draw += windowSystem.Draw;
        Service.Interface.UiBuilder.Draw += OverlayWindow.Draw;
        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;

        MajorUpdater.Enable();
        Watcher.Enable();
        CountDown.Enable();
    }


    private void ClientState_TerritoryChanged(object sender, ushort e)
    {
        if (Service.Conditions[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty]
            || Service.Conditions[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56]
            || Service.Conditions[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty95]
            || Service.Conditions[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundToDuty97]) return;
        CommandController.AttackCancel();
    }

    public void Dispose()
    {
        Service.CommandManager.RemoveHandler(_command);
        Service.CommandManager.RemoveHandler(_autoCommand);
        Service.Interface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Service.Interface.UiBuilder.Draw -= windowSystem.Draw;
        Service.Interface.UiBuilder.Draw -= OverlayWindow.Draw;
        Service.IconReplacer.Dispose();

        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;

        MajorUpdater.Dispose();
        Watcher.Dispose();
        CountDown.Dispose();
    }

    private void OnOpenConfigUi()
    {
        configWindow.IsOpen = true;
    }

    private void AttackObject(string command, string arguments)
    {
        string[] array = arguments.Split();

        if (array.Length > 0)
        {
            CommandController.DoAutoAttack(array[0]);
        }
    }

    private void OnCommand(string command, string arguments)
    {
        string[] array = arguments.Split();

        if (IconReplacer.AutoAttackConfig(array[0], array.Length > 1 ? array[1] : array[0]))
            OpenConfigWindow();
    }

    internal static void OpenConfigWindow()
    {
        configWindow.Toggle();
    }
}
