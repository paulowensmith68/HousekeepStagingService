Imports System.Xml
Imports System.IO
Imports System.Text
Imports MySql.Data
Imports MySql.Data.MySqlClient
Public Class HousekeepDatabaseClass

    ' Holds the connection string to the database used.
    Public connectionString As String = globalConnectionString

    ' Holds name of key to delete data from after inserts
    Public strDeleteKeyType As String = ""

    'Vars used for output message
    Private insertCount As Integer = 0
    Private updateCount As Integer = 0

    'Vars used to control cursor
    Public intCursorCount As Integer = 0

    ' Store last event id so we ony log each event
    Dim intLastEventId As Integer = 0

    Public Sub IdentifyEvents()
        '-----------------------------------------------------------------------*
        ' Sub Routine parameters                                                *
        ' -----------------------                                               *
        '-----------------------------------------------------------------------*
        Dim cno As MySqlConnection = New MySqlConnection(connectionString)
        Dim dr As MySqlDataReader
        Dim cmd As New MySqlCommand

        ' Reset cursor counter
        intCursorCount = 0

        ' /----------------------------------------------------------------\
        ' | MySql Select                                                   |
        ' | Identify all past events                                       |
        ' \----------------------------------------------------------------/
        cmd.CommandText = "SELECT ev.`id`, ev.name, ev.`startDate` FROM Event As ev " &
                                 "WHERE ev.`status_type` = ""finished"" AND " &
                                 "ev.startdate < str_to_date(@todaysDate, '%Y-%m-%d %H:%i:%s') " &
                                 "LIMIT @limit "

        ' Get start and end date
        Dim currentDateTime As DateTime = DateTime.UtcNow
        Dim strCurrentDate As String

        ' Calculate start date/time and To date/time
        Dim centralEuropeZoneId As String = "Central Europe Standard Time"
        Dim centralEuropeZone As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(centralEuropeZoneId)
        strCurrentDate = TimeZoneInfo.ConvertTimeFromUtc(currentDateTime, centralEuropeZone).ToString("yyyy-MM-dd" + " 00:00:00")

        cmd.Parameters.AddWithValue("todaysDate", strCurrentDate)
        cmd.Parameters.AddWithValue("limit", My.Settings.LimitEventRows)

        cmd.Connection = cno

        Try
            cno.Open()
            dr = cmd.ExecuteReader

            If dr.HasRows Then

                While dr.Read()

                    ' Increment counter
                    intCursorCount = intCursorCount + 1

                    Dim intEventId As Integer = dr.GetInt64(0)
                    Dim strEventName As String = dr.GetString(1)
                    Dim dtStartDate As DateTime = dr.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss")

                    ' Log each event
                    If intEventId = intLastEventId Then
                        ' Continue
                    Else
                        intLastEventId = intEventId
                        gobjEvent.WriteToEventLog("HousekeepDatabaseClass:  Processing event: " + strEventName + " which was on: " + dtStartDate.ToString("yyyy-MM-dd HH:mm:ss"))
                    End If

                    ' Delete data
                    DeleteBettingOffers(intEventId)
                    DeleteOutcomes(intEventId)
                    DeleteBettingOffersStaging(intEventId)
                    DeleteOutcomesStaging(intEventId)
                    DeleteEvent(intEventId)

                End While ' End: Outer Loop

            End If

            ' Close the Data reader
            dr.Close()

            gobjEvent.WriteToEventLog("HousekeepDatabaseClass:  Processed Events, number of rows: " + intCursorCount.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("HousekeepDatabaseClass:  Processing Events exception: " + ex.Message, EventLogEntryType.Error)

        Finally
            cno.Close()
        End Try

    End Sub

    Private Sub DeleteBettingOffers(eventId As Integer)

        Dim myConnection As New MySqlConnection(connectionString)
        Dim cmd As New MySqlCommand
        cmd.CommandText = "DELETE bo.* FROM bettingoffer AS bo " &
                                "INNER JOIN outcome AS ou ON ou.`id`=bo.`outcomeFK` " &
                                "INNER JOIN event AS ev ON ev.`id` =ou.`objectFK` " &
                                "WHERE ou.`object`=""event"" AND ou.`objectFK`=@eventId "
        cmd.Connection = myConnection
        cmd.Parameters.Add(New MySqlParameter("eventId", eventId))

        Try

            myConnection.Open()
            Dim rowAffected As Integer = cmd.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteBettingOffers : Deleted betting offers rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteBettingOffers : Delete betting offers rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            cmd.Dispose()
            myConnection.Close()

        End Try

    End Sub

    Private Sub DeleteOutcomes(eventId As Integer)

        Dim myConnection As New MySqlConnection(connectionString)
        Dim cmd As New MySqlCommand
        cmd.CommandText = "DELETE ou.* FROM outcome AS ou " &
                                "INNER JOIN event AS ev ON ev.`id` =ou.`objectFK` " &
                                "WHERE ou.`object`=""event"" AND ou.`objectFK`=@eventId "
        cmd.Connection = myConnection
        cmd.Parameters.Add(New MySqlParameter("eventId", eventId))

        Try

            myConnection.Open()
            Dim rowAffected As Integer = cmd.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteOutcomes : Deleted outcome rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteOutcomes : Delete outcome rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            cmd.Dispose()
            myConnection.Close()

        End Try

    End Sub

    Private Sub DeleteBettingOffersStaging(eventId As Integer)

        Dim myConnection As New MySqlConnection(connectionString)
        Dim cmd As New MySqlCommand
        cmd.CommandText = "DELETE bxn.* FROM bookmaker_xml_nodes AS bxn " &
                                "INNER JOIN outcome AS ou ON ou.`id`=bxn.`outcome_Id` " &
                                "INNER JOIN event AS ev ON ev.`id` =ou.`objectFK` " &
                                "WHERE ou.`object`=""event"" AND ou.`objectFK`=@eventId "
        cmd.Connection = myConnection
        cmd.Parameters.Add(New MySqlParameter("eventId", eventId))

        Try

            myConnection.Open()
            Dim rowAffected As Integer = cmd.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteBettingOffers : Deleted betting offers from bookmaker_xml_nodes rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteBettingOffers : Delete betting offers from bookmaker_xml_nodes rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            cmd.Dispose()
            myConnection.Close()

        End Try

    End Sub

    Private Sub DeleteOutcomesStaging(eventId As Integer)

        'Delete id from saved_xml (log table)
        Dim myConnection As New MySqlConnection(connectionString)
        Dim cmd As New MySqlCommand
        cmd.CommandText = "DELETE bxn.* FROM bookmaker_xml_nodes AS bxn " &
                          "WHERE  bxn.`event_id`=@eventId "
        cmd.Connection = myConnection
        cmd.Parameters.Add(New MySqlParameter("eventId", eventId))

        Try

            myConnection.Open()
            Dim rowAffected As Integer = cmd.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteOutcomesStaging : Deleted outcomes from bookmaker_xml_nodes rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteOutcomesStaging : Delete outcomes from bookmaker_xml_nodes rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            cmd.Dispose()
            myConnection.Close()

        End Try

    End Sub

    Private Sub DeleteEvent(eventId As Integer)

        Dim myConnection As New MySqlConnection(connectionString)
        Dim myCommand As New MySqlCommand("delete from event where `id`=@id")
        myCommand.CommandType = CommandType.Text
        myCommand.Connection = myConnection
        myCommand.Parameters.Add(New MySqlParameter("id", eventId))

        Try

            myConnection.Open()
            Dim rowAffected As Integer = myCommand.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteEvent : Deleted event rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteEvent : Delete event rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            myCommand.Dispose()
            myConnection.Close()

        End Try

    End Sub

    Public Sub DeleteStaleBettingOffersStaging()

        Dim myConnection As New MySqlConnection(connectionString)
        Dim cmd As New MySqlCommand
        cmd.CommandTimeout = 360

        cmd.CommandText = "DELETE FROM bookmaker_xml_nodes " &
                          "WHERE nodeName = ""bettingoffer"" AND " &
                          "ut < str_to_date(@dateTime, '%Y-%m-%d %H:%i:%s') " &
                          "LIMIT @limit "

        ' Get start and end date
        Dim currentDateTime As DateTime = DateTime.UtcNow
        Dim strParamDate As String
        Dim twoHoursAgoDateTime As DateTime

        ' Calculate start date/time and To date/time
        Dim centralEuropeZoneId As String = "UTC"
        Dim centralEuropeZone As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(centralEuropeZoneId)
        Dim intMinutes = My.Settings.HousekeepStaleBetsAfterMins * -1
        twoHoursAgoDateTime = currentDateTime.AddMinutes(intMinutes)
        strParamDate = TimeZoneInfo.ConvertTimeFromUtc(twoHoursAgoDateTime, centralEuropeZone).ToString("yyyy-MM-dd HH:mm:ss")

        cmd.Parameters.AddWithValue("dateTime", strParamDate)
        cmd.Parameters.AddWithValue("limit", My.Settings.LimitStaleRows)
        cmd.Connection = myConnection

        Try

            myConnection.Open()
            Dim rowAffected As Integer = cmd.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteStaleBettingOffersStaging : Deleted stale BettingOffers Staging rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteStaleBettingOffersStaging : Delete stale BettingOffers Staging rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            cmd.Dispose()
            myConnection.Close()

        End Try

    End Sub

    Public Sub DeleteStaleOutcomesStaging()

        Dim myConnection As New MySqlConnection(connectionString)
        Dim cmd As New MySqlCommand
        cmd.CommandTimeout = 360

        cmd.CommandText = "DELETE FROM bookmaker_xml_nodes " &
                          "WHERE nodeName = ""outcome"" AND " &
                          "ut < str_to_date(@dateTime, '%Y-%m-%d %H:%i:%s') " &
                          "LIMIT @limit "

        ' Get start and end date
        Dim currentDateTime As DateTime = DateTime.UtcNow
        Dim strParamDate As String
        Dim twoHoursAgoDateTime As DateTime

        ' Calculate start date/time and To date/time
        Dim centralEuropeZoneId As String = "UTC"
        Dim centralEuropeZone As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(centralEuropeZoneId)
        Dim intMinutes = My.Settings.HousekeepStaleBetsAfterMins * -1
        twoHoursAgoDateTime = currentDateTime.AddMinutes(intMinutes)
        strParamDate = TimeZoneInfo.ConvertTimeFromUtc(twoHoursAgoDateTime, centralEuropeZone).ToString("yyyy-MM-dd HH:mm:ss")

        cmd.Parameters.AddWithValue("dateTime", strParamDate)
        cmd.Parameters.AddWithValue("limit", My.Settings.LimitStaleRows)
        cmd.Connection = myConnection

        Try

            myConnection.Open()
            Dim rowAffected As Integer = cmd.ExecuteNonQuery()

            gobjEvent.WriteToEventLog("DeleteStaleOutcomesStaging : Deleted stale Outcomes Staging rows: " + rowAffected.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("DeleteStaleOutcomesStaging : Delete stale Outcomes Staging rows failed: " + ex.Message, EventLogEntryType.Error)

        Finally

            cmd.Dispose()
            myConnection.Close()

        End Try

    End Sub

End Class
