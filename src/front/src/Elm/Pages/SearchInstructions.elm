module Pages.SearchInstructions exposing (..)

import Available exposing (Available, availableDecoder)
import Bootstrap.Button as Button
import Bootstrap.Card as Card
import Bootstrap.Card.Block as Block
import Bootstrap.Form.Input as Input
import Bootstrap.Form.Select as Select
import Bootstrap.Utilities.Flex as Flex
import Endpoints exposing (Endpoint(..), instructioIdToCover)
import Html exposing (Html, div, text)
import Html.Attributes exposing (alt, class, href, src, style, value)
import Http
import Json.Decode as D
import Json.Decode.Pipeline exposing (custom, required)
import Main exposing (AuthResponse, ModelBase(..), MsgBase(..), UserRole(..), baseApplication, initBase, mapCmd, updateBase)
import Multiselect as Multiselect
import NavBar exposing (instructionsLink, viewNav)
import Utils exposing (buildQuery, chunkedView, decodeId, fillParent, flex, flex1, intersect, largeCentered, mediumMargin, smallMargin)
import Webdata exposing (WebData(..), viewWebdata)



--model


type alias Model =
    ModelBase View


type alias View =
    { available : WebData Available
    , instructions : WebData (List Instruction)
    , selectedGroup : String
    , selectedDescription : String
    , selectedTitle : String
    , showAdd : Bool
    }


type alias Instruction =
    { id : String
    , title : String
    , description : String
    , imageUrl : Maybe String
    }



--update


type LocalMsg
    = NoOp
    | GotAvailable (Result Http.Error Available)
    | TitleChanged String
    | DescriptionChanged String
    | GroupChanged String
    | GotSearch (Result Http.Error (List Instruction))


type alias Msg =
    MsgBase LocalMsg


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

                triggerSearch withModel =
                    search withModel.selectedTitle withModel.selectedDescription withModel.selectedGroup auth.token
            in
            case msg of
                GotAvailable (Ok res) ->
                    let
                        newModel =
                            { model | available = Loaded res, selectedGroup = res.groups |> Multiselect.getValues |> List.head |> Maybe.withDefault ( "", "" ) |> Tuple.first }
                    in
                    ( authed newModel, triggerSearch newModel )

                GotAvailable (Err err) ->
                    ( authed { model | available = Error }, Cmd.none )

                GotSearch (Ok res) ->
                    ( authed { model | instructions = Loaded res }, Cmd.none )

                GotSearch (Err err) ->
                    ( authed { model | instructions = Error }, Cmd.none )

                GroupChanged groupId ->
                    let
                        newModel =
                            { model | selectedGroup = groupId }
                    in
                    ( authed newModel, triggerSearch newModel )

                TitleChanged title ->
                    let
                        newModel =
                            { model | selectedTitle = title }
                    in
                    ( authed newModel, triggerSearch newModel )

                DescriptionChanged desc ->
                    let
                        newModel =
                            { model | selectedDescription = desc }
                    in
                    ( authed newModel, triggerSearch newModel )

                NoOp ->
                    noOp

        _ ->
            ( m, Cmd.none )



--commands


getAvailable : String -> Cmd Msg
getAvailable token =
    Endpoints.getAuthed token Dicts (Http.expectJson GotAvailable availableDecoder) Nothing |> mapCmd


search : String -> String -> String -> String -> Cmd Msg
search title description groupId token =
    let
        expect =
            Http.expectJson GotSearch (searchDecoder token)

        queryParams =
            [ ( "GroupId", groupId ), ( "Title", title ), ( "Description", description ) ]
    in
    Endpoints.getAuthedQuery (buildQuery queryParams) token FindInstructions expect Nothing |> mapCmd


searchDecoder : String -> D.Decoder (List Instruction)
searchDecoder token =
    D.field "items" (D.list <| searchItemDecoder token)


searchItemDecoder : String -> D.Decoder Instruction
searchItemDecoder token =
    D.succeed Instruction
        |> required "id" decodeId
        |> required "title" D.string
        |> required "description" D.string
        |> custom (coverDecoder token)


coverDecoder : String -> D.Decoder (Maybe String)
coverDecoder token =
    D.field "hasCover" D.bool |> D.andThen (coverImageDecoder token)


coverImageDecoder token hasCover =
    if hasCover then
        D.map (\id -> Just (instructioIdToCover token id)) (D.field "id" decodeId)

    else
        D.succeed Nothing



--view


view model =
    viewLocal model |> Html.map Main


viewLocal : Model -> Html LocalMsg
viewLocal model =
    viewNav model (Just instructionsLink) viewPage


viewPage : AuthResponse -> View -> Html LocalMsg
viewPage resp page =
    viewWebdata page.available (viewMain (intersect [ Producer, Manager ] resp.roles) page)


viewMain : Bool -> View -> Available -> Html LocalMsg
viewMain isProducer page av =
    let
        btnView =
            if page.showAdd then
                div [ flex, Flex.row, style "flex" "0.5", mediumMargin ] [ Button.linkButton [ Button.attrs [ href "/instructions/add" ], Button.primary ] [ text "Create" ] ]

            else
                div [] []
    in
    div ([ Flex.col, flex ] ++ fillParent)
        [ btnView
        , div [ flex, Flex.row, flex1 ] (viewSelections page av)
        , div [ flex, Flex.row, style "flex" "8" ]
            [ viewWebdata page.instructions (viewInstructions isProducer) ]
        ]


viewSelections : View -> Available -> List (Html LocalMsg)
viewSelections page av =
    let
        groups =
            Multiselect.getValues av.groups

        viewGroup group =
            Select.item [ value <| Tuple.first group ] [ text <| Tuple.second group ]

        colAttrs =
            [ flex, Flex.col, flex1, smallMargin ]
    in
    [ div colAttrs
        [ div largeCentered [ text "Group" ]
        , Select.select [ Select.onChange GroupChanged ] (List.map viewGroup groups)
        ]
    , div colAttrs
        [ div largeCentered [ text "Title" ]
        , Input.text
            [ Input.value page.selectedTitle, Input.onInput TitleChanged ]
        ]
    , div colAttrs
        [ div largeCentered [ text "Description" ]
        , Input.text
            [ Input.value page.selectedDescription, Input.onInput DescriptionChanged ]
        ]
    ]


viewInstructions : Bool -> List Instruction -> Html LocalMsg
viewInstructions isProducer ins =
    chunkedView 3 (viewInstruction isProducer) ins


viewInstruction : Bool -> Instruction -> Html LocalMsg
viewInstruction isProducer ins =
    let
        editBtn =
            if isProducer then
                Button.linkButton [ Button.primary, Button.attrs [ href <| "/instructions/" ++ ins.id ++ "/edit", smallMargin ] ] [ text "Edit" ]

            else
                div [] []
    in
    Card.config [ Card.attrs (fillParent ++ [ style "flex" "1" ]) ]
        |> Card.header [ class "text-center" ]
            [ Html.img ([ src (Maybe.withDefault "" ins.imageUrl), alt "No cover for this instruction" ] ++ fillParent) []
            ]
        |> Card.block []
            [ Block.titleH4 [] [ text ins.title ]
            , Block.text [] [ text ins.description ]
            , Block.custom <|
                div [ flex, Flex.row, Flex.justifyEnd, Flex.alignItemsCenter ]
                    [ editBtn
                    , Button.linkButton [ Button.primary, Button.attrs [ href <| "/instructions/" ++ ins.id ] ] [ text "Open Full" ]
                    ]
            ]
        |> Card.view


init : Maybe AuthResponse -> D.Value -> ( Model, Cmd Msg )
init resp flags =
    let
        shouldShow =
            case resp of
                Just res ->
                    intersect [ Producer, Manager ] res.roles

                Nothing ->
                    False
    in
    initBase [ Producer, Consumer, Manager ] (View Loading Loading "" "" "" shouldShow) (\res -> getAvailable res.token) resp


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
