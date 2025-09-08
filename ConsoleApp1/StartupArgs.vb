Namespace Global.GreyDogSoftware.Extensions
    Public Class StartupArgs
        Private Shared CurrArgs As Dictionary(Of String, List(Of String))
        Private Const ArgumentDelimiter As String = "-"
        Public Shared Property CaseSensitive As Boolean = False
        Public Shared ReadOnly Property CurrentArgs As Dictionary(Of String, List(Of String))
            Get
                ParseArgs()
                Return CurrArgs
            End Get
        End Property
        Private Shared Sub ParseArgs()
            CurrArgs = New Dictionary(Of String, List(Of String))
            If Environment.GetCommandLineArgs().Count > 1 Then
                Dim ArgsIndex As Integer = 0
                For Each Arg In Environment.GetCommandLineArgs()
                    If ArgsIndex > 0 Then
                        If Arg.Length >= 1 Then
                            If CurrArgs.Count = 0 And Arg.Substring(0, 1) <> ArgumentDelimiter Then
                                CurrArgs.Add("[empty]", New List(Of String))
                                CurrArgs.Last.Value.Add(Arg)
                            Else
                                If Not CaseSensitive Then
                                    Arg = Arg.ToLower
                                End If
                                If Arg.Substring(0, 1) = ArgumentDelimiter Then
                                    ' Is a new command.
                                    CurrArgs.Add(Arg, New List(Of String))
                                Else
                                    ' Is a command parameter
                                    CurrArgs.Last.Value.Add(Arg)
                                End If
                            End If
                        End If
                    End If
                    ArgsIndex += 1
                Next
            End If
        End Sub
    End Class
End Namespace

