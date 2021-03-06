﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class World : FContainer
{
    public FTmxMap map;
    public FTilemap collision;

    List<Vector2> spawnPoints = new List<Vector2>();
    List<Door> doorList = new List<Door>();
    List<TutorialText> tutorialList = new List<TutorialText>();
    List<CheckPoint> checkPointList = new List<CheckPoint>();
    List<BaseGameObject> enemies = new List<BaseGameObject>();
    List<Pickup> pickups = new List<Pickup>();

    FContainer backgroundLayer;
    FContainer playerLayer;
    FContainer foregroundLayer;

    FSprite whiteOverlaySprite;
    FLabel mapNameLabel;

    public Player p;

    public World()
        : base()
    {
        map = new FTmxMap();

        whiteOverlaySprite = new FSprite(C.whiteElement);
        whiteOverlaySprite.alpha = 0;
        whiteOverlaySprite.color = Color.black;
        whiteOverlaySprite.width = Futile.screen.width;
        whiteOverlaySprite.height = Futile.screen.height;

        mapNameLabel = new FLabel(C.NormalFont, "");
        mapNameLabel.y = -Futile.screen.halfHeight + 40;

    }

    public override void HandleAddedToStage()
    {
        Futile.instance.SignalUpdate += Update;
        base.HandleAddedToStage();
    }

    public override void HandleRemovedFromStage()
    {
        Futile.instance.SignalUpdate -= Update;
        base.HandleRemovedFromStage();
    }

    public void loadMap(string mapName)
    {
        backgroundLayer = new FContainer();
        playerLayer = new FContainer();
        foregroundLayer = new FContainer();

        doorList = new List<Door>();
        tutorialList = new List<TutorialText>();

        map = new FTmxMap();
        FCamObject camera = C.getCameraInstance();
        map.clipNode = camera;
        map.LoadTMX("Maps/" + mapName);
        map.actualMapName = mapName;

        camera.setWorldBounds(new Rect(0, -map.height, map.width, map.height));

        collision = (FTilemap)map.getLayerNamed("Collision");

        addObjects(map);

        FTilemap fg = (FTilemap)map.getLayerNamed("FG");
        map.RemoveChild(fg);

        backgroundLayer.AddChild(map);

        this.AddChild(backgroundLayer);
        this.AddChild(playerLayer);
        this.AddChild(fg);
        camera.AddChild(mapNameLabel);
        camera.AddChild(whiteOverlaySprite);
        camera.MoveToFront();


    }

    public void checkDoor(Player p)
    {
        foreach (Door door in doorList)
            if (door.checkPlayer(p))
            {
                C.transitioning = true;
                C.getCameraInstance().MoveToFront();
                Go.to(whiteOverlaySprite, C.transitioningTime, new TweenConfig().floatProp("alpha", 1.0f).setEaseType(EaseType.QuadOut).onComplete((AbstractTween t) =>
                {
                    if (door.toMap.CompareTo("end") == 0)
                    {
                        this.RemoveFromContainer();
                        C.getCameraInstance().RemoveAllChildren();
                        FPageManager.getInstance().transitionOn(new Credits());
                        return;
                    }
                    this.RemoveAllChildren();
                    loadMap(door.toMap);
                    spawnPlayer(p, door.toDoor);
                    mapNameLabel.alpha = 1;
                    Go.killAllTweensWithTarget(mapNameLabel);
                    mapNameLabel.text = map.mapName;
                    Go.to(whiteOverlaySprite, C.transitioningTime, new TweenConfig().floatProp("alpha", 0).setEaseType(EaseType.QuadIn).onComplete((AbstractTween t2) => { C.transitioning = false; Go.to(mapNameLabel, 2.0f, new TweenConfig().floatProp("alpha", 0).setDelay(1.0f).setEaseType(EaseType.QuadIn)); }));
                }));
                break;
            }
    }

    public void loadLastSave()
    {
        if (String.IsNullOrEmpty(C.lastSave.mapName))
        {

            C.getCameraInstance().MoveToFront();
            Go.to(whiteOverlaySprite, C.transitioningTime, new TweenConfig().floatProp("alpha", 1.0f).setEaseType(EaseType.QuadOut).onComplete((AbstractTween t) =>
            {
                p.setDrillPower(1);
                p.maxJumpsLeft = 1;
                C.doorsBroken.Clear();
                this.RemoveAllChildren();
                foreach (TutorialText tut in tutorialList)
                    tut.removeSelf();
                loadMap(C.startMap);
                spawnPlayer(p, C.startDoor);
                p.setHealth(8);
                mapNameLabel.alpha = 1;
                Go.killAllTweensWithTarget(mapNameLabel);
                mapNameLabel.text = map.mapName;
                Go.to(whiteOverlaySprite, C.transitioningTime, new TweenConfig().floatProp("alpha", 0).setEaseType(EaseType.QuadIn).onComplete((AbstractTween t2) => { C.transitioning = false; Go.to(mapNameLabel, 2.0f, new TweenConfig().floatProp("alpha", 0).setDelay(1.0f).setEaseType(EaseType.QuadIn)); }));
            }));
        }
        else
        {

            C.getCameraInstance().MoveToFront();
            Go.to(whiteOverlaySprite, C.transitioningTime, new TweenConfig().floatProp("alpha", 1.0f).setEaseType(EaseType.QuadOut).onComplete((AbstractTween t) =>
            {
                p.setDrillPower(C.lastSave.drillLevel);
                p.maxJumpsLeft = C.lastSave.jumpBoots ? 2 : 1;
                C.doorsBroken = new List<KeyValuePair<string, int>>(C.lastSave.openedDoors.AsEnumerable());
                foreach (TutorialText tut in tutorialList)
                    tut.removeSelf();
                this.RemoveAllChildren();
                loadMap(C.lastSave.mapName);
                if (checkPointList.Count > 0)
                    checkPointList[0].spawnPlayer(p);
                spawnPlayer(p);
                mapNameLabel.alpha = 1;
                Go.killAllTweensWithTarget(mapNameLabel);
                mapNameLabel.text = map.mapName;
                Go.to(whiteOverlaySprite, C.transitioningTime, new TweenConfig().floatProp("alpha", 0).setEaseType(EaseType.QuadIn).onComplete((AbstractTween t2) => { C.transitioning = false; Go.to(mapNameLabel, 2.0f, new TweenConfig().floatProp("alpha", 0).setDelay(1.0f).setEaseType(EaseType.QuadIn)); }));
            }));
        }
    }

    private void Update()
    {
        if (C.transitioning)
            return;

        foreach (Pickup pickup in pickups)
        {
            if (p.contains(pickup.GetPosition()))
            {
                pickup.pickup(p);
                pickups.Remove(pickup);
                break;
            }
        }

        foreach (CheckPoint checkPoint in checkPointList)
            checkPoint.checkPlayer(p);

        foreach (TutorialText tutText in tutorialList)
            tutText.checkPlayer(p);

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].health <= 0)
            {
                enemies.RemoveAt(i);
                i--;
            }
            if (enemies[i] is SpaceGhost)
            {
                for (int j = i + 1; j < enemies.Count; j++)
                {
                    if (enemies[j] is SpaceGhost)
                    {
                        ((SpaceGhost)enemies[i]).checkOtherGhost((SpaceGhost)enemies[j]);
                    }
                }
            }
        }

    }


    private void addObjects(FTmxMap map)
    {
        checkPointList.Clear();
        pickups.Clear();
        enemies.Clear();
        int doorNumber = 1;
        foreach (XMLNode node in map.objects)
        {
            if (node.attributes.ContainsKey("gid"))
            {

                int frameNum = int.Parse(node.attributes["gid"]) - (map.getTilesetFirstIDForID(int.Parse(node.attributes["gid"])) - 1);

                switch (frameNum)
                {
                    case 1:
                        addSpaceGhost(node, 2);
                        break;
                    case 2:
                        addSlime(node);
                        break;
                    case 3:
                        addJumpBoots(node);
                        break;
                    case 4:
                        addSpaceGhost(node, 1);
                        break;
                    case 5:
                        addTiki(node);
                        break;
                    case 6:
                        addWall(node, 1, doorNumber++);
                        break;
                    case 7:
                        addDrill(node, 2);
                        break;
                    case 8:
                        addDrill(node, 3);
                        break;
                    case 9:
                        addWall(node, 2, doorNumber++);
                        break;
                    case 10:
                        addWall(node, 3, doorNumber++);
                        break;
                    case 11:
                        addSpaceGhost(node, 3);
                        break;
                    case 12:
                        addCheckPoint(node);
                        break;
                }
            }
            else
                if (node.attributes["type"].ToLower().CompareTo("door") == 0)
                {
                    //Is a door object
                    Vector2 pos = new Vector2(float.Parse(node.attributes["x"]), float.Parse(node.attributes["y"]));
                    float width = float.Parse(node.attributes["width"]);
                    float height = float.Parse(node.attributes["height"]);
                    string toMap = "";
                    string toDoor = "";
                    foreach (XMLNode property in ((XMLNode)node.children[0]).children)
                    {
                        if (property.attributes["name"].ToLower().CompareTo("tomap") == 0)
                            toMap = property.attributes["value"];
                        else
                            if (property.attributes["name"].ToLower().CompareTo("todoor") == 0)
                                toDoor = property.attributes["value"];
                    }
                    doorList.Add(new Door(pos, width, height, node.attributes["name"], toMap, toDoor));
                }
                else
                {
                    if (node.attributes["type"].ToLower().CompareTo("tutorial") == 0)
                    {
                        //Tutorial text box
                        Vector2 pos = new Vector2(float.Parse(node.attributes["x"]), float.Parse(node.attributes["y"]));
                        float width = float.Parse(node.attributes["width"]);
                        float height = float.Parse(node.attributes["height"]);
                        string tutText = "";
                        foreach (XMLNode child in node.children)
                            if (child.tagName.CompareTo("properties") == 0)
                                foreach (XMLNode property in child.children)
                                {
                                    if (property.attributes["name"].ToLower().CompareTo("text") == 0)
                                        tutText = property.attributes["value"];
                                    
                                }

                        tutorialList.Add(new TutorialText(pos, width, height, tutText));
                    }
                }
        }
    }

    private void addCheckPoint(XMLNode node)
    {
        CheckPoint checkPoint = new CheckPoint(new Vector2(float.Parse(node.attributes["x"]), -float.Parse(node.attributes["y"])));
        checkPointList.Add(checkPoint);
    }

    private void addSlime(XMLNode node)
    {
        float rotation = 0;
        if (node.children.Count > 0)
            foreach (XMLNode properties in ((XMLNode)node.children[0]).children)
            {
                if (properties.attributes["name"].CompareTo("rotation") == 0)
                    rotation = int.Parse(properties.attributes["value"]);
            }
        WallSlime enemy = new WallSlime(new Vector2(int.Parse(node.attributes["x"]) + map.tileWidth / 2, -int.Parse(node.attributes["y"]) + map.tileHeight / 2), rotation);
        enemy.setWorld(this);
        enemies.Add(enemy);
        playerLayer.AddChild(enemy);
    }

    private void addJumpBoots(XMLNode node)
    {
        if (p == null || p.maxJumpsLeft == 1)
        {
            JumpBoots boot = new JumpBoots(new Vector2(int.Parse(node.attributes["x"]) + map.tileWidth / 2, -int.Parse(node.attributes["y"]) + map.tileHeight / 2));
            pickups.Add(boot);
            playerLayer.AddChild(boot);
        }
    }

    private void addDrill(XMLNode node, int level)
    {

        if (p == null || p.drillLevel < level)
        {
            DrillPowerUp drill = new DrillPowerUp(new Vector2(int.Parse(node.attributes["x"]) + map.tileWidth / 2, -int.Parse(node.attributes["y"]) + map.tileHeight / 2), level);
            pickups.Add(drill);
            playerLayer.AddChild(drill);
        }
    }

    private void addSpaceGhost(XMLNode node, int level)
    {
        SpaceGhost ghost = new SpaceGhost(new Vector2(int.Parse(node.attributes["x"]) + map.tileWidth / 2, -int.Parse(node.attributes["y"]) + map.tileHeight / 2), level);
        ghost.setWorld(this);
        enemies.Add(ghost);
        playerLayer.AddChild(ghost);
    }

    private void addWall(XMLNode node, int level, int doorNumber)
    {
        bool isFlipped = false;
        foreach (XMLNode child in node.children)
            if (child.tagName.CompareTo("properties") == 0)
                foreach (XMLNode property in child.children)
                    if (property.attributes["name"].CompareTo("isFlipped") == 0)
                        isFlipped = true;

        Wall wall = new Wall(new Vector2(int.Parse(node.attributes["x"]) + map.tileWidth / 2, -int.Parse(node.attributes["y"]) + map.tileHeight), level, doorNumber);
        if (isFlipped)
            wall.flip();
        foreach (KeyValuePair<string, int> pair in C.doorsBroken)
        {
            if (pair.Key.CompareTo(this.map.actualMapName) == 0)
                if (pair.Value == doorNumber)
                    wall.breakDoor();
        }
        wall.setWorld(this);
        enemies.Add(wall);
        playerLayer.AddChild(wall);

    }

    private void addTiki(XMLNode node)
    {
        int numTikis = 3;
        foreach (XMLNode nodeChild in node.children)
            if (nodeChild.tagName.CompareTo("Properties") == 0)
                foreach (XMLNode property in nodeChild.children)
                    if (property.attributes["name"].ToLower().CompareTo("numTikis") == 0)
                        numTikis = int.Parse(property.attributes["value"]);

        Tiki tiki = new Tiki(new Vector2(int.Parse(node.attributes["x"]) + map.tileWidth / 2, -int.Parse(node.attributes["y"]) + map.tileHeight / 2), numTikis);
        tiki.setWorld(this);
        enemies.Add(tiki);
        playerLayer.AddChild(tiki);
    }

    public void spawnPlayer(Player p, String toDoor)
    {
        this.p = p;
        C.getCameraInstance().follow(p);

        playerLayer.AddChild(p);
        foreach (Door door in doorList)
        {
            if (door.Name.ToLower().CompareTo(toDoor.ToLower()) == 0)
            {
                door.spawnPlayer(p);
                break;
            }
        }
        p.lastY = p.y;
        p.lastX = p.x;
        p.setWorld(this);
        p.resetCounts();
    }

    public void spawnPlayer(Player p)
    {
        this.p = p;
        C.getCameraInstance().follow(p);
        playerLayer.AddChild(p);
        p.lastX = p.x;
        p.lastY = p.y;
        p.setWorld(this);
        p.resetCounts();
    }
}

