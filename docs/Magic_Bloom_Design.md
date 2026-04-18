# Magic Bloom - Game Design va Technical Design

## 1. Tong quan du an

**Magic Bloom** la mot game puzzle 2D theo co che "water sort" / "color sort", duoc phat trien bang `Unity 6` va `C#`.
Nguoi choi chon mot ong, do phan mau tren cung sang ong khac theo dung luat, va muc tieu la dua toan bo cac ong ve trang thai dong nhat mau hoac rong.

Du an hien tai tap trung vao:

- Core gameplay do mau giua cac ong.
- He thong level tao san thanh `ScriptableObject`.
- Score/combo va coin reward sau moi level.
- Power-up `Undo`, `Add Tube`, `Shuffle`.
- UI panel cho in-game, setting, shop, win screen.
- Shop cosmetic va goi mua hang mo rong.
- Audio, VFX va animation bang `DOTween`.

Game co dinh huong mobile ro rang qua cac dau hieu trong code:

- Tuong tac bang pointer/tap.
- `PlayerPrefs` cho save nhanh.
- Toggle haptic.
- Link `Rate Us`, `Privacy Policy`.
- Shop va IAP flow dang duoc scaffold san.

## 2. Muc tieu trai nghiem

Game huong toi mot vong lap ngan, de vao choi, kho dung dan, va co phan thuong thuc thi ngay:

- Input don gian: cham de chon va do.
- Phan hoi visual ro: ong nhac len, xoay, do mau, dong nap.
- Cam giac combo: hoan thanh lien tiep se tang sao.
- Co "cua thoat" khi bi ket: undo, them ong rong, shuffle.
- Sau moi level co coin reward va shop de tao cam giac tien trinh.

## 3. Core loop gameplay

Vong lap nguoi choi:

1. Vao level.
2. Quan sat bo cuc ong va cac lop mau.
3. Chon ong nguon.
4. Chon ong dich hop le.
5. He thong animate viec do mau va cap nhat model.
6. Neu hoan thanh mot ong thi cong diem, tang combo, phat VFX.
7. Lap lai den khi tat ca ong deu hoan thanh hoac rong.
8. Nhap win state, tinh sao, tinh coin, cho claim hoac x2/x3/x5 coin.
9. Qua level tiep theo.

Dieu kien thang:

- Moi `TubeModel` deu o mot trong hai trang thai:
- `Rules.IsCompleted(tube) == true`
- Hoac `tube.isEmpty == true`

Khong thay fail state / lose state that trong runtime code hien tai. Day la game giai do dang free-form, khong gioi han nuoc di.

## 4. Luat choi chi tiet

He thong luat nam trong `Rules.cs`.

Mot luot do hop le khi:

- Ong nguon khong rong.
- Nguon va dich khong phai cung mot ong.
- Ong nguon khong phai ong da day va da hoan thanh mot mau.
- Ong dich con du cho trong de nhan toan bo top segment.
- Neu dich khong rong thi top color cua dich phai trung voi top color cua nguon.

Mot top segment duoc xem la mot block mau lien mach, vi du `Red x3`.
Game khong do tung don vi 1 cell, ma do tron top segment.

He qua cua rule nay:

- Gameplay de doc va phu hop voi water-sort genre.
- Do kho den tu cach bo tri block, khong den tu physics.
- Undo co the record gon bang `fromIndex`, `toIndex`, `ColorSegment`.

## 5. Mo hinh du lieu gameplay

### 5.1 TubeData

`TubeData` la du lieu level thuan, luu mang `layers`.
No duoc dung de serialize level trong `LevelDataSO`.

Vai tro:

- Dai dien layout level o asset.
- Chuyen doi tu mang mau sang danh sach `ColorSegment`.

### 5.2 TubeModel

`TubeModel` la model runtime cua mot ong.

Thuoc tinh chinh:

- `capacity`
- `totalColor`
- `segments`
- `filledAmount`
- `freeAmount`
- `isEmpty`

`TubeModel` la "nguon su that" cho logic gameplay trong runtime.

### 5.3 ColorSegment

`ColorSegment` gom:

- `colorId`
- `Amount`

No la don vi logic trung gian giua level data va rendering.

## 6. Luong runtime va kien truc tong the

Kien truc hien tai theo huong:

- `Singleton services` de dieu phoi.
- `Plain data model` cho logic.
- `View/controller split` cho presentation va input.
- `ScriptableObject` cho level content.

So do luong runtime:

```text
GameManager
  -> chuyen sang InGame
  -> LevelManager.LoadLevel(level)
      -> tai LevelDataSO tu Resources/Levels
      -> tao TubeModel cho moi tube
      -> instantiate TubeView prefab
      -> bind model vao view
      -> auto layout

Nguoi choi click TubeView
  -> TubeViewClick
  -> TubeController.OnTubeClicked
      -> Rules.CanPour / Rules.Pour
      -> LevelManager.RecordMove
      -> TubeView animate
      -> ComboTracker / ScoreManager / UIManager cap nhat

Neu level complete
  -> UIManager.OnGameStateChanged(Win)
  -> WinPanelUI.ShowResult(...)
  -> user claim reward
  -> UIManager.GoNextLevel()
  -> LevelManager.LoadNextLevel()
```

## 7. Phan ra module hien tai

### 7.1 Core domain

Thu muc: `Assets/Game/Scripts/Core`

Thanh phan:

- `TubeModel`: model runtime cho ong.
- `TubeData`: du lieu level serialize.
- `Rules`: luat do mau va dieu kien complete.
- `ColorPalette`: map `ColorId -> Color`.
- `Constant`: key save, coin reward, star base, gioi han layout.

Danh gia:

- Day la lop domain kha gon.
- Logic duoc tach khoi UI kha ro.
- Tuy nhien ten class va namespace chua duoc chuan hoa hoan toan.

### 7.2 Gameplay orchestration

Thanh phan:

- `GameManager`
- `LevelManager`
- `TubeController`
- `UndoManager`
- `ScoreManager`
- `ComboTracker`

Trach nhiem:

- `GameManager`: state tong the `Shop / InGame / Win`, khoi dong game.
- `LevelManager`: tai level, tao model/view, auto layout, them ong rong, shuffle mode, undo trigger.
- `TubeController`: xu ly chon ong, chuyen select, pour animation, win check scheduling.
- `UndoManager`: luu stack nuoc di va hoan tac.
- `ScoreManager`: tinh tong sao va xep hang 1-3 sao cuoi level.
- `ComboTracker`: tang combo moi khi hoan thanh ong va reset sau mot khoang thoi gian.

Danh gia:

- Day la trung tam cua runtime.
- `LevelManager` dang ganh kha nhieu vai tro: level loading, layout, extra tube, shuffle state, undo entry point.
- Kieu to chuc nay hop voi project nho, nhung se can tach them neu du an tiep tuc mo rong.

### 7.3 Presentation layer

Thanh phan:

- `TubeView`
- `TubeViewClick`
- `TubeZigZagVFX`
- `VFXPathMover`
- `ScoreBurstStarUI`
- `PowerUpButton`
- `InGamePanelUI`
- `SettingPanelUI`
- `ShopPanelUI`
- `WinPanelUI`

Trach nhiem:

- `TubeView`: render cac segment, animate phan top, animate nap ong, apply cap skin.
- `TubeViewClick`: nhan su kien pointer click va chuyen cho controller.
- `InGamePanelUI`: level text, combo meter, score, nut power-up, score burst effect.
- `WinPanelUI`: hien level, sao, coin reward, multiplier slider.
- `ShopPanelUI`: tab packages va cosmetics.
- `SettingPanelUI`: toggle sound/music/haptic, replay, rate, privacy.

Danh gia:

- Layer view tuong doi doc lap voi rule.
- Presentation duoc cham chut tot bang animation va VFX.
- `TubeView` dang chua ca rendering, skinning va animation, nhung van chap nhan duoc cho scope hien tai.

### 7.4 Commerce va persistence

Thanh phan:

- `SaveService`
- `InventoryService`
- `ShopService`
- `ShopSaveService`

Trach nhiem:

- `SaveService`: luu so luong item gameplay va reset tong.
- `InventoryService`: cache runtime cho `Undo`, `AddTube`, `ShuffleTube`.
- `ShopService`: coins, equip, unlock cosmetics, scaffold IAP reward grant.
- `ShopSaveService`: wrapper `PlayerPrefs` cho coin, ownership, equip, no-ads, IAP state.

Danh gia:

- Du de demo economy va shop loop.
- Logic save con phan tan giua 2 service save rieng.
- Chua co mot save schema versioned hoac migration.

## 8. Level design va content pipeline

### 8.1 Cau truc level

Moi level la mot `LevelDataSO` gom:

- `level`
- `capacity`
- `totalColor`
- `viewType`
- `isBreatherLevel`
- `isMilestoneLevel`
- `targetMoves`
- `totalTransitions`
- `topBlockers`
- `estMoves`
- `tubes`

Trong repo hien tai co `50` level asset trong `Assets/Game/Levels`.

### 8.2 Sinh level

Pipeline sinh level nam trong `LevelGenerator.cs`.

Cac buoc:

1. Tao `LevelConfig` tu level number bang seed co dinh.
2. Tinh `colorCount`, `capacity`, `mixDepth`, `emptyTubes`, `viewType`.
3. Tao mot solved layout.
4. Chia tube thanh cac segment pattern.
5. Swap cac segment cung do dai giua cac tube de tron bo cuc.
6. Retry toi da 60 lan neu layout xau.
7. Fallback ve solved state neu that bai.

Uu diem:

- Deterministic theo level number.
- Co quy tac `breather level` va `milestone level`.
- Co khung metadata de phat trien them difficulty analytics.

Luu y:

- `viewType`, `targetMoves`, `totalTransitions`, `topBlockers`, `estMoves` hien chua thay duoc su dung trong runtime gameplay.
- Nghia la content pipeline da co dat cho difficulty system, nhung phan gameplay scene chua khai thac het.

### 8.3 Cong cu editor

`LevelBatchGenerator` la `EditorWindow` cho phep:

- Generate all levels.
- Regenerate single level.
- Luu asset vao `Assets/Game/Levels`.

Day la diem manh cho workflow designer/programmer, vi khong can tao tung level bang tay.

## 9. Score, combo va reward design

### 9.1 Score

Score duoc tinh theo "star points" trong `ScoreManager`.

Quy tac:

- Hoan thanh 1 ong binh thuong: `+5`.
- Neu combo > 1: reward = `BASE_STARS * combo + BASE_STARS`.

### 9.2 Combo

`ComboTracker` tang combo moi khi mot ong duoc hoan thanh.
Combo reset sau `5s` neu khong tiep tuc complete.

Tac dung design:

- Khuyen khich nguoi choi sap xep de chain completion.
- Bien mot puzzle co ban thanh he thong co nhan to performance.

### 9.3 Xep hang cuoi level

`WinPanelUI` hien 1-3 sao dua tren ty le star point dat duoc so voi muc toi da ly thuyet.

Coin reward:

- `1 sao -> 10 coin`
- `2 sao -> 25 coin`
- `3 sao -> 50 coin`

Win screen co multiplier slider:

- Vung he so: `x2 | x3 | x5 | x3 | x2`

Day la mot co che reward presentation phu hop mobile casual.

## 10. Power-up design

Game co 3 power-up runtime:

- `Undo`
- `AddTube`
- `ShuffleTube`

### 10.1 Undo

- Record bang `MoveRecord`.
- Hoan tac dua tren segment vua do.
- Khong replay animation nguoc, chi refresh model/view.

### 10.2 Add Tube

- Them ong rong vao runtime.
- Gioi han boi `maxExtraTubes = 2`.
- Sau khi them se auto layout lai toan bo board.

### 10.3 Shuffle Tube

- Bat `shuffle select mode`.
- Nguoi choi chon 1 ong hop le de tron thu tu segment trong ong do.
- Sau shuffle se merge lai neu co 2 segment cung mau lien nhau.

Danh gia design:

- Bo 3 power-up nay rat hop voi water-sort.
- Gia tri cua tung item de hieu va tac dong truc tiep len puzzle state.

## 11. UI/UX flow

Game su dung mot `UIManager` de dong/mo panel theo state.

State hien tai:

- `Shop`
- `InGame`
- `Win`

Panel:

- `InGamePanelUI`
- `SettingPanelUI`
- `ShopPanelUI`
- `WinPanelUI`

Luot di UI:

- Start game -> `InGamePanelUI`
- Bam shop -> `ShopPanelUI`
- Bam setting -> `SettingPanelUI`
- Win level -> `WinPanelUI`

`InGamePanelUI` co mot phan implementation kha tot:

- Subscribe event score.
- Subscribe inventory thay doi.
- Subscribe undo history.
- Subscribe shuffle mode va extra tube state.
- Co object pool cho score burst stars.

Dieu nay cho thay UI da co y thuc event-driven o muc vua du.

## 12. Shop va he thong cosmetic

### 12.1 Capabilities da co

- Coin wallet.
- Ownership cua cosmetic.
- Equip `Tube Cap`.
- Equip `Background`.
- Package/IAP scaffold.
- `No Ads` flag.

### 12.2 Cosmetic tabs

- `SkinCapTabUI`: preview cap, mua/equip/claim theo trang thai.
- `BackgroundTabUI`: preview background, action button theo trang thai.
- `CosmeticSlotUI`: slot config + indicator.
- `PackageSlotUI`: scaffold mua package.

### 12.3 Muc do hoan thien hien tai

Da noi vao runtime:

- `Tube Cap` da duoc ap vao `TubeView` qua `ShopService.OnCapChanged`.

Chua thay noi day du vao runtime gameplay:

- `Background` moi thay o UI va data shop, chua thay mot gameplay/background renderer lang nghe `OnBgChanged`.
- `EquippedSkin` ton tai trong `ShopService` nhung chua thay duoc su dung.

Noi cach khac, shop da co form day du, nhung runtime application hien hoan chinh nhat o phan cap.

## 13. Audio va VFX

### 13.1 Audio

`AudioManager` quan ly:

- 1 `AudioSource` cho music.
- 1 `AudioSource` cho SFX.
- Dictionary clip theo ten.
- Cooldown de tranh spam SFX.
- Save setting bang `PlayerPrefs`.

Audio cue da duoc map ro cho:

- Button click
- Bottle up/down/pull/close
- Star reward
- Undo / add tube / shuffle
- Combo high
- Win

### 13.2 VFX va animation

Du an dung `DOTween` cho:

- Nhac ong len/xuong.
- Arc move va rotate khi pour.
- Dong nap ong khi complete.
- Score burst stars.
- Star reveal trong win panel.
- Pointer slider trong win panel.

`TubeZigZagVFX` tao duong chay zig-zag khi ong complete.
Day la lop "juice" quan trong, giup puzzle co cam giac thoa man hon.

## 14. Persistence design

Save hien tai dung `PlayerPrefs`.

Noi dung duoc luu:

- So luong item gameplay.
- Coins.
- Cosmetic ownership.
- Cosmetic equipped state.
- No Ads.
- IAP purchase state.
- Audio settings.
- Haptic setting.

Danh gia:

- Hop voi project casual nho va prototype/mobile demo.
- Chua phu hop neu can cloud save, versioning, anti-cheat, analytics, cross-device sync.

## 15. Scene composition hien tai

Tu build settings va scene wiring co the thay:

- Build scene chinh: `Assets/Game/Scenes/SampleScene.unity`
- Scene co it nhat:
- `GameManager`
- `LevelManager`
- `UIManager`
- `Canvas`

Scene nay da bind san:

- `LevelManager -> tuberPrefab`
- `LevelManager -> spawnRoot`
- `UIManager -> shopPanel / settingPanel / winPanel / inGamePanel`
- `AudioManager -> musicSource / sfxSource`

Mo hinh nay cho thay du an van la `single-scene game architecture`, phu hop casual puzzle.

## 16. Cong nghe su dung

Engine va framework:

- `Unity 6.0.4f1`
- `C#`
- `Unity UGUI`
- `TextMesh Pro`
- `ScriptableObject`
- `PlayerPrefs`
- `Unity EditorWindow` cho tooling noi bo

Package / thu vien:

- `com.unity.feature.2d`
- `com.unity.inputsystem`
- `com.unity.test-framework`
- `com.unity.ugui`
- `DOTween`

Ghi chu:

- Input runtime hien tai chu yeu di qua `IPointerClickHandler` va `Button`, du goi Input System package van chua thay mot action-driven input architecture cho gameplay.

## 17. Diem manh kien truc hien tai

- De doc, de debug, phu hop scope solo/team nho.
- Domain logic puzzle duoc tach khoi UI kha ro.
- Co tooling sinh level hang loat.
- Co deterministic generation theo level number.
- Co event UI o muc vua du, khong qua nang.
- Co layer polish tot: animation, VFX, sound, score burst.
- Co dat san mo rong economy/shop.

## 18. Diem yeu va rui ro ky thuat

### 18.1 Singleton nhieu

Rat nhieu module dung `Singleton<T>`.
Voi scope hien tai van chap nhan duoc, nhung ve sau se kho test, kho thay the va kho tach scene/system.

### 18.2 State progression chua dong bo hoan chinh

Code hien tai co `Constant.LEVEL_KEY`, nhung chua thay flow cap nhat level progression sau khi thang.
Dieu nay co nghia:

- Qua level tiep theo trong session van chay.
- Nhung progression luu dai han trong `PlayerPrefs` chua duoc noi day du.

Them vao do, `BackgroundTabUI` dang doc `"CurrentLevel"` thay vi `Constant.LEVEL_KEY`, gay nguy co lock/unlock khong dong nhat.

### 18.3 Metadata level chua duoc khai thac het

`viewType`, `targetMoves`, `totalTransitions`, `topBlockers`, `estMoves` da ton tai nhung chua thay runtime dung den.

### 18.4 Shop runtime chua noi het

- `Tube Cap`: da ap dung.
- `Background`: moi o data/UI.
- `EquippedSkin`: chua thay runtime consumer.

### 18.5 Testing con rat mong

Da co package test framework, nhung trong repo chu yeu moi thay `WinPanelUITest.cs` theo kieu test thu cong trong scene.
Chua thay automated unit/integration test that su cho:

- Rule system
- Level generation
- Save/load
- Shop unlock flow

## 19. De xuat huong nang cap neu phat trien tiep

### 19.1 Tach ro application flow

Nen tach 3 lop:

- `GameFlow` cho state va progression.
- `LevelRuntime` cho state trong 1 level.
- `MetaProgression` cho save coins, unlocked cosmetics, highest level.

### 19.2 Dong bo level progression

Can them flow:

- Win level -> cap nhat `highest unlocked level`
- Shop / cosmetic unlock doc cung mot key
- Replay va continue dung chung progression state

### 19.3 Tich hop metadata level vao gameplay

Co the dung:

- `viewType` de an mot phan ong/layer that.
- `targetMoves` de cham 3 sao theo so nuoc di.
- `estMoves` de can bang do kho.
- `isMilestoneLevel` de them reward / theme / special VFX.

### 19.4 Giam tai cho LevelManager

Tach bot:

- `BoardLayoutService`
- `PowerUpRuntimeService`
- `LevelContentRepository`

Neu lam vay, `LevelManager` se tro thanh application coordinator gon hon.

### 19.5 Bo sung test

Uu tien test cho:

- `Rules.CanPour`
- `Rules.Pour`
- `UndoManager.Undo`
- `LevelGenerator.GenerateTubes`
- `ShopService.GetStatus`

## 20. Tong ket ngan

Magic Bloom la mot puzzle game casual co core gameplay ro rang, animation tot, va kien truc vua du cho mot project solo/nhom nho.
Du an da co nen tang kha day du: gameplay, level pipeline, score/combo, power-up, UI, shop, save, audio.
Phan can hoan thien nhat neu muon nang cap thanh san pham day du la progression persistence, su dung metadata level trong runtime, va noi tron ven cosmetic system vao scene gameplay.
