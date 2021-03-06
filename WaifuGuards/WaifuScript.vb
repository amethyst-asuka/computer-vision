﻿Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports GTA.Math
Imports GTA.Native

Public Class WaifuScript : Inherits Script

    ReadOnly names$() = WaifuList.LoadNames
    ReadOnly rand As New Random

    Public Shared ReadOnly favoriteWeapons As WeaponHash() = {
        WeaponHash.HeavySniper,
        WeaponHash.Railgun,
        WeaponHash.MicroSMG,
        WeaponHash.SpecialCarbine,
        WeaponHash.CombatPDW,
        WeaponHash.SMG
    }

    Friend ReadOnly waifuGuards As New List(Of Waifu)
    Friend ReadOnly events As New List(Of TickEvent(Of WaifuScript))
    Friend ReadOnly pendingQueue As New PendingQueue(Of WaifuScript)

    Dim toggleKillable As Boolean = False
    Dim toggleGangGroupMode As Boolean = False

    Sub New()
        If WaifuList.IsWaifusMegaPackInstalled Then
            Call events.Add(New FollowPlayer)
            Call events.Add(New AssistPlayer)
            ' Call events.Add(New StopAttackPartner)
        Else
            ' Given warning message
            UI.ShowSubtitle("[Waifus mega pack] not found, you can download this mod from: https://zh.gta5-mods.com/player/lolis-and-waifus-mega-pack-blz")
        End If

        ' guards.SeparationRange = 1000
        ' guards.Add(Game.Player.Character, leader:=True)
        Game.Player.Character.CurrentPedGroup.SeparationRange = 2000
    End Sub

    Private Sub spawnWaifu(name As String)
        Dim waifu As New Waifu(name, Me)
        Dim nextHash = rand.Next(favoriteWeapons.Length)
        Dim randWeapon As WeaponHash = favoriteWeapons(nextHash)

        If waifu.MarkDeletePending Then
            UI.ShowSubtitle($"Missing model [{name}]...")
        Else
            Call waifu.TakeAction(
                Sub(waifuPed As Ped)
                    waifuPed.Weapons.Give(randWeapon, 99999, True, True)
                    waifuPed.RelationshipGroup = Game.Player.Character.RelationshipGroup
                    waifuPed.IsInvincible = True
                    waifuPed.AddBlip()

                    ' Call guards.Add(waifuPed, leader:=False)

                    With waifuPed.CurrentBlip
                        .Scale = 0.7!
                        .Name = "Waifu"
                        .Color = BlipColor.Blue
                    End With

                    If toggleGangGroupMode Then
                        Call AddToGangGroup(waifuPed, name)
                    End If
                End Sub)

            Call waifuGuards.Add(waifu)
            Call UI.ShowSubtitle($"[{name}] joined.")
        End If
    End Sub

    Private Sub AddToGangGroup(waifuPed As Ped, name$)
        Dim myHandle = New InputArgument() {Game.Player.Character.Handle}
        Dim myHash% = [Function].Call(Of Integer)(Hash._0xF162E133B4E7A675, myHandle)
        Dim myGuard = New InputArgument() {waifuPed.Handle, myHash}

        Call [Function].Call(Hash._0x9F3480FE65DB31B5, myGuard)
        Call waifuPed.Task.ClearAllImmediately()
        Call [Function].Call(Hash._0x4CF5F55DAC3280A0, New InputArgument() {waifuPed, &HC350, 0})
        Call [Function].Call(Hash._0x971D38760FBC02EF, New InputArgument() {waifuPed, 1})

        Call UI.ShowSubtitle($"Bring [{name}] as one of your group member.")
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Function offsetAroundMe() As Vector3
        Return New Vector3(rand.Next(-10, 10), rand.Next(-10, 10), 0)
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Sub Pending(action As PendingEvent(Of WaifuScript))
        Call pendingQueue.Add(action)
    End Sub

    Dim toggleIdleCameraOn As Boolean = False

    ''' <summary>
    ''' Press key ``Y`` for spawn a waifu.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Waifus_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Y Then
            Call spawnWaifu(names(rand.Next(0, names.Length)))
        ElseIf e.KeyCode = Keys.NumPad9 Then
            ' spawn all
            For Each name As String In names
                If waifuGuards.Count < 6 Then
                    ' too many peds will makes GTAV crashed.
                    Call spawnWaifu(name)
                Else
                    Exit For
                End If
            Next
        ElseIf e.KeyCode = Keys.U Then
            ' union all your waifus
            ' force waifu stop current task and guard player immediately
            Call FollowPlayer.PlayerUnion(Me, Function() False)
        ElseIf e.KeyCode = Keys.Delete Then
            toggleKillable = Not toggleKillable
        ElseIf e.KeyCode = Keys.E Then
            Dim vehicle = World.GetClosestVehicle(Game.Player.Character.Position, 20)

            ' Calls closest enter your vehicle
            If Not vehicle Is Nothing AndAlso Game.Player.Character.IsInVehicle(vehicle) Then
                Dim minDistanceWaifu = waifuGuards _
                    .Where(Function(w)
                               ' try to make all waifus enter your bus
                               Return Not w.IsInVehicle(vehicle)
                           End Function) _
                    .OrderBy(Function(w) w.DistanceToPlayer) _
                    .FirstOrDefault

                If Not minDistanceWaifu Is Nothing Then
                    Call minDistanceWaifu.TakeAction(
                        Sub(actions As Tasks)
                            Call UI.ShowSubtitle($"[{minDistanceWaifu.Name}], get into my vehicle.")

                            If minDistanceWaifu.IsInCombat Then
                                Call actions.ClearAllImmediately()
                            End If

                            Call actions.EnterVehicle(vehicle, VehicleSeat.Passenger)
                        End Sub)
                End If
            Else
                ' play an emotion to a waifu who is in front of you
                ' Game.Player.Character.Task.PlayAnimation()
            End If
        ElseIf e.KeyCode = Keys.I Then
            With TryCast(events.Where(Function(ev) TypeOf ev Is FollowPlayer).FirstOrDefault, FollowPlayer)
                If Not .IsNothing Then
                    If .StopFollow Then
                        Call .Start()
                    Else
                        Call .Stop(waifuGuards)
                    End If
                End If
            End With
        ElseIf e.KeyCode = Keys.Add Then
            If toggleIdleCameraOn Then
                Dim pos = Game.Player.Character.Position
                pos.Z += 1
                Game.Player.Character.Position = pos
            End If
        ElseIf e.KeyCode = Keys.Subtract Then
            If toggleIdleCameraOn Then
                Dim pos = Game.Player.Character.Position
                pos.Z -= 1
                Game.Player.Character.Position = pos
            End If
        End If
    End Sub

    Private Sub Waifus_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        For Each [event] As TickEvent(Of WaifuScript) In events
            Call [event].Tick(Me)
        Next

        ' find out a engaged ped nearby the player
        Dim nearby As Ped = World _
           .GetNearbyPeds(Game.Player.Character.Position, 50) _
           .Where(Function(p)
                      Dim isPlayer As Boolean = Game.Player.Character Is p
                      Dim isWaifu As Boolean = waifuGuards.Any(Function(waifu) waifu = p)
                      Return (Not isPlayer) AndAlso p.IsInCombat AndAlso (Not isWaifu)
                  End Function) _
           .FirstOrDefault

        For Each waifu As Waifu In waifuGuards.ToArray
            If Not waifu.IsDead Then
                If waifu.IsShootByPlayer AndAlso toggleKillable Then
                    Call waifu.Kill()

                ElseIf Not nearby Is Nothing AndAlso waifu.IsAvailable Then
                    Call waifu.TakeAction(
                        Sub(actions As Tasks)
                            Call actions.FightAgainst(nearby)
                        End Sub)
                End If

                ' Call waifu.StopAttack(Game.Player.Character)

                ' removes too far away peds for release memory
                Dim distance# = waifu.DistanceToPlayer

                If distance > 200 Then
                    Call UI.ShowSubtitle($"Delete [{waifu.Name}]: Too far away from you.")
                    Call waifu.Delete()
                ElseIf distance > 30 Then
                    ' not working as expected....
                    ' the code cause the ped freezed
                    '
                    ' Call waifu.TakeAction(
                    '     Sub(actions As Tasks)
                    '         ' Call actions.ClearAllImmediately()
                    '         Call actions.RunTo(Game.Player.Character.Position)
                    '     End Sub)
                End If

                If waifu.IsInCombat Then
                    Dim target As Ped = waifu.Target

                    If Not target Is Nothing AndAlso target.IsDead Then
                        Call waifu.TakeAction(
                            Sub(actions As Tasks)
                                ' This function still caused the ped stop unexpected
                                ' Call actions.ClearAllImmediately()
                                Call actions.ClearAll()

                                If Not nearby Is Nothing Then
                                    Call actions.FightAgainst(nearby)
                                End If
                            End Sub)

                        ' not working as expected....
                        ' the code is also cause the ped freezed

                        ' ElseIf Not target Is Nothing AndAlso waifu.DistanceTo(target) > 50 Then
                        ' If too far away with the target, given up fight with him
                        'Call waifu.TakeAction(
                        '    Sub(actions As Tasks)
                        '        Call actions.ClearAllImmediately()

                        '        If Not nearby Is Nothing Then
                        '            Call actions.FightAgainst(nearby)
                        '        End If
                        '    End Sub)
                    End If
                End If
            Else
                If Not waifu.MarkDeletePending Then
                    Call waifu.Kill()
                End If
            End If
        Next

        Call pendingQueue.Tick(Me)
    End Sub
End Class
