module Pages.Users exposing (..)

import Bootstrap.Button as Button
import Bootstrap.ButtonGroup as ButtonGroup exposing (CheckboxButtonItem)
import Bootstrap.Card as Card
import Bootstrap.Card.Block as Block
import Bootstrap.Form.Input as Input
import Bootstrap.Utilities.Flex as Flex
import Endpoints exposing (Endpoint(..), getAuthedQuery)
import Html exposing (Html, div, text)
import Html.Attributes exposing (href, style)
import Http
import Json.Decode as D
import Json.Decode.Pipeline exposing (custom, required)
import Main exposing (AuthResponse, ModelBase(..), UserRole(..), baseApplication, convertRole, convertRoleStr, initBase, roleToStr, rolesDecoder)
import Multiselect as Multiselect
import NavBar exposing (usersLink, viewNav)
import Utils exposing (buildQuery, chunkedView, fillParent, flatten, flex, flex1, largeCentered, mediumMargin, smallMargin, unique)
import Webdata exposing (WebData(..), viewWebdata)



--model


type alias Model =
    ModelBase View


type alias View =
    { users : WebData (List User)
    , selectedRoles : Multiselect.Model
    , selectedName : Maybe String
    , selectedPhone : Maybe String
    }


type alias User =
    { name : String
    , contact : String
    , roles : List UserRole
    , login : String
    }



--update


type Msg
    = NoOp
    | GotUsers (Result Http.Error (List User))
    | SelectedRole Multiselect.Msg
    | ChangedName String
    | ChangedPhone String
    | CheckedRole UserRole String


update : Msg -> Model -> ( Model, Cmd Msg )
update msg m =
    let
        noOp =
            ( m, Cmd.none )
    in
    case m of
        Authorized auth model ->
            let
                authed =
                    Authorized auth

                authedSearch =
                    searchForUsers auth.token

                mapRoles roles =
                    case Multiselect.getSelectedValues roles of
                        [] ->
                            Nothing

                        arr ->
                            Just (List.map (\val -> Tuple.first val |> String.toInt |> Maybe.withDefault -1 |> convertRole) arr)

                searchCmd name phone roles =
                    authedSearch name phone (mapRoles roles)
            in
            case msg of
                GotUsers (Ok res) ->
                    ( authed <| { model | users = Loaded res }, Cmd.none )

                GotUsers (Err err) ->
                    ( authed <| { model | users = Error }, Cmd.none )

                SelectedRole roleMsg ->
                    let
                        ( subModel, subCmd, _ ) =
                            Multiselect.update roleMsg model.selectedRoles

                        updatedModel =
                            { model | selectedRoles = subModel }

                        search =
                            if Multiselect.getSelectedValues model.selectedRoles /= Multiselect.getSelectedValues updatedModel.selectedRoles then
                                searchCmd updatedModel.selectedName updatedModel.selectedPhone updatedModel.selectedRoles

                            else
                                Cmd.none
                    in
                    ( authed updatedModel, Cmd.batch [ Cmd.map SelectedRole subCmd, search ] )

                ChangedName name ->
                    ( authed { model | selectedName = Just name }, searchCmd (Just name) model.selectedPhone model.selectedRoles )

                ChangedPhone phone ->
                    ( authed { model | selectedPhone = Just phone }, searchCmd model.selectedName (Just phone) model.selectedRoles )

                CheckedRole role login ->
                    noOp

                NoOp ->
                    noOp

        _ ->
            ( m, Cmd.none )



--view


view : Model -> Html Msg
view model =
    viewNav model (Just usersLink) viewPage


viewPage : AuthResponse -> View -> Html Msg
viewPage resp page =
    div ([ flex, Flex.col ] ++ fillParent)
        [ div [ flex1, mediumMargin ]
            [ Button.linkButton [ Button.primary, Button.attrs ([ href "/user/add" ] ++ largeCentered) ] [ text "Create User" ]
            ]
        , div [ style "flex" "2", flex, Flex.row ]
            [ viewInput (Input.text [ Input.onInput ChangedName ]) "Name"
            , viewInput (Input.text [ Input.onInput ChangedPhone ]) "Mobile Number"
            , viewInput (Html.map SelectedRole <| Multiselect.view page.selectedRoles) "Roles"
            ]
        , div [ flex, Flex.row, style "flex" "16", style "overflow-y" "scroll" ]
            [ viewWebdata page.users (chunkedView 3 <| viewUser resp.roles)
            ]
        ]


viewUser : List UserRole -> User -> Html Msg
viewUser viewerRoles user =
    Card.config []
        |> Card.header largeCentered
            [ div largeCentered [ text user.name ]
            ]
        |> Card.block []
            [ Block.titleH4 largeCentered [ text user.login ]
            , Block.titleH4 largeCentered [ text user.contact ]
            , Block.custom <|
                userRolesBtns user.login user.roles viewerRoles
            ]
        |> Card.view


userRolesBtns : String -> List UserRole -> List UserRole -> Html Msg
userRolesBtns login userRoles viewerRoles =
    let
        maxViewer =
            Maybe.withDefault -1 <| List.maximum <| List.map roleToNumber viewerRoles

        canEdit role =
            roleToNumber role <= maxViewer

        roles =
            [ Consumer, Producer, Manager ]
    in
    ButtonGroup.checkboxButtonGroup [ ButtonGroup.attrs fillParent ]
        (List.map
            (\role -> btnView (List.member role userRoles) role (canEdit role) login)
            roles
        )


btnView : Bool -> UserRole -> Bool -> String -> CheckboxButtonItem Msg
btnView checked role canCheck login =
    ButtonGroup.checkboxButton
        checked
        [ Button.primary, Button.onClick <| CheckedRole role login, Button.disabled <| not canCheck ]
        [ text <| roleToStr role ]


viewInput : Html msg -> String -> Html msg
viewInput input desc =
    div [ flex, flex1, Flex.col, Flex.alignItemsCenter, smallMargin ]
        [ div largeCentered [ text desc ]
        , input
        ]



--init


init : Maybe AuthResponse -> D.Value -> ( Model, Cmd Msg )
init resp flags =
    let
        initialCmd res =
            searchForUsers res.token Nothing Nothing Nothing

        roleToVisible role =
            case role of
                Consumer ->
                    [ ( "1", "Consumer" ) ]

                Producer ->
                    [ ( "1", "Consumer" ), ( "2", "Producer" ) ]

                Manager ->
                    [ ( "1", "Consumer" ), ( "2", "Producer" ), ( "3", "Manager" ) ]

        rolesList =
            case resp of
                Just res ->
                    unique <| flatten <| List.map roleToVisible res.roles

                Nothing ->
                    []

        multiSelect =
            Multiselect.initModel rolesList "roles" Multiselect.Show
    in
    initBase [ Producer, Consumer, Manager ] (View Loading multiSelect Nothing Nothing) initialCmd resp



--cmds


searchForUsers : String -> Maybe String -> Maybe String -> Maybe (List UserRole) -> Cmd Msg
searchForUsers token name contact roles =
    let
        expect =
            Http.expectJson GotUsers usersDecoder

        rolesList =
            List.map (Tuple.mapSecond (List.map roleToNumber)) (justOrEmpty "roles" roles)

        rolesQuery =
            case
                List.head
                    (List.map (\rolePair -> List.map (\role -> ( Tuple.first rolePair, role )) (Tuple.second rolePair)) rolesList)
            of
                Just i ->
                    i

                Nothing ->
                    []

        queryList =
            justOrEmpty "name" name ++ justOrEmpty "phone" contact ++ List.map (Tuple.mapSecond String.fromInt) rolesQuery
    in
    getAuthedQuery (buildQuery queryList) token SearchUsers expect Nothing


roleToNumber : UserRole -> Int
roleToNumber role =
    case role of
        Consumer ->
            1

        Producer ->
            2

        Manager ->
            3


usersDecoder : D.Decoder (List User)
usersDecoder =
    D.field "items" (D.list userDecoder)


userDecoder : D.Decoder User
userDecoder =
    D.succeed User
        |> required "fullName" D.string
        |> required "mobile" D.string
        |> custom (rolesDecoder (D.field "roleCodes" <| D.list D.int))
        |> required "login" D.string


justOrEmpty : String -> Maybe a -> List ( String, a )
justOrEmpty key val =
    case val of
        Just value ->
            [ ( key, value ) ]

        Nothing ->
            []



--subs


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