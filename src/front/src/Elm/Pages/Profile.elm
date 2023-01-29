module Pages.Profile exposing (..)

import Bootstrap.Button as Button
import Bootstrap.Form.Input as Input
import Bootstrap.ListGroup as ListGroup
import Bootstrap.Utilities.Flex as Flex
import Endpoints exposing (Endpoint(..), postAuthed)
import Html exposing (Html, div, text)
import Html.Attributes exposing (class, style)
import Http
import Json.Decode as D
import Json.Encode as E
import Main exposing (AuthResponse, ModelBase(..), MsgBase(..), UserRole(..), baseApplication, initBase, mapCmd, roleToStr, updateBase)
import NavBar exposing (viewNav)
import Utils exposing (SubmittedResult(..), fillParent, flex, flexCenter, largeCentered, mediumMargin, smallMargin, submittedDecoder)
import Webdata exposing (WebData(..), viewWebdata)



--model


type alias Model =
    ModelBase View


type alias View =
    { password : String
    , result : Maybe (WebData SubmittedResult)
    }



--update


type LocalMsg
    = NoOp
    | NewPasswordChanged String
    | ChangePass
    | GotChangePass (Result Http.Error SubmittedResult)


type alias Msg =
    MsgBase LocalMsg


update : Msg -> Model -> ( Model, Cmd Msg )
update =
    updateBase updateLocal


updateLocal : LocalMsg -> Model -> ( Model, Cmd Msg )
updateLocal msg m =
    let
        noOp =
            ( m, Cmd.none )
    in
    case m of
        Authorized auth model ->
            let
                authed =
                    Authorized auth

                updateModel newModel =
                    ( authed newModel, Cmd.none )
            in
            case msg of
                NewPasswordChanged pass ->
                    updateModel { model | password = pass, result = Nothing }

                ChangePass ->
                    ( authed { model | result = Just Loading }, submitCommand auth.token model.password )

                GotChangePass (Ok res) ->
                    updateModel { model | result = Just <| Loaded res }

                GotChangePass (Err res) ->
                    updateModel { model | result = Just Error }

                NoOp ->
                    noOp

        _ ->
            ( m, Cmd.none )



--commands


submitCommand : String -> String -> Cmd Msg
submitCommand token pass =
    let
        decoder =
            submittedDecoder (D.field "success" D.bool) (D.field "message" D.string)

        expect =
            Http.expectJson GotChangePass decoder

        body =
            Http.jsonBody (E.object [ ( "password", E.string pass ) ])
    in
    postAuthed token ChangePassword body expect Nothing |> mapCmd



--view


view : Model -> Html Msg
view model =
    viewNav model Nothing viewPage


viewPage : AuthResponse -> View -> Html Msg
viewPage resp page =
    let
        shouldDisableInput =
            case page.result of
                Just Loading ->
                    True

                _ ->
                    False
    in
    div ([ flex, Flex.row, mediumMargin ] ++ fillParent ++ flexCenter)
        [ div [ flex, Flex.col, class "modal__container" ]
            [ div largeCentered [ text "My Roles" ]
            , ListGroup.ul (List.map (\role -> ListGroup.li [] [ text <| roleToStr role ]) resp.roles)
            , div largeCentered [ text "New Password" ]
            , Input.password
                [ Input.value page.password
                , Input.onInput NewPasswordChanged
                , Input.attrs
                    [ mediumMargin
                    , style "width" "unset"
                    ]
                , Input.disabled shouldDisableInput
                ]
                |> Html.map Main
            , buttonView page |> Html.map Main
            , Button.linkButton [ Button.danger, Button.onClick <| Navigate "/login/new", Button.attrs [ mediumMargin ] ] [ text "Logout" ]
            ]
        ]


buttonView page =
    case page.result of
        Just res ->
            viewWebdata res viewResult

        Nothing ->
            Button.button [ Button.primary, Button.attrs [ mediumMargin ], Button.onClick ChangePass ]
                [ text "Change password"
                ]


viewResult : SubmittedResult -> Html msg
viewResult res =
    case res of
        SubmittedSuccess msg ->
            div (largeCentered ++ [ class "text-success" ]) [ text msg ]

        SubmittedFail msg ->
            div (largeCentered ++ [ class "text-warning" ]) [ text msg ]


init : Maybe AuthResponse -> D.Value -> ( Model, Cmd Msg )
init resp flags =
    initBase [ Producer, Consumer, Manager ] (View "" Nothing) (\res -> Cmd.none) resp


subscriptions : Model -> Sub Msg
subscriptions model =
    Sub.none


main : Program D.Value Model Msg
main =
    baseApplication
        { init = init
        , view = view
        , update = update
        , subscriptions = subscriptions
        }
