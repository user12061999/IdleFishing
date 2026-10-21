# IDLE FISHING — GAME DESIGN DOCUMENT 2.0
**30 Stage Progression · 5 Worlds · Hybrid Casual + Idle Tycoon · Unity/URP**  
**Version:** 1.0 (Thiết kế đề xuất, chưa triển khai / playtest)  
**Nguồn gốc:** `/mnt/data/GAMEPLAY_ARCHITECTURE_SPEC.md` do chủ dự án cung cấp; tên class và các mechanic hiện có lấy từ đó.  
**Companion data:** `IdleFishing_30_Stages_Data.json` chứa 30 cấu hình stage ở dạng JSON trung gian để Codex chuyển thành ScriptableObject.

> **Phân biệt sự thật vs đề xuất.** Tài liệu gốc xác nhận 5 World và gimmick đặc trưng; các stage 1–30, Fish Behavior mở rộng, Boss phase, cooldown, mục tiêu, chỉ số pacing/economy, species bổ sung và tầng retention bên dưới là **đề xuất thiết kế**. Không được mô tả những thành phần này là đã có trong repo nếu chưa kiểm tra code.

## 1. TÓM TẮT THIẾT KẾ

- **Fantasy:** xây dựng một bến cá ngày càng lớn, tự động hóa các công đoạn, nhưng vẫn có cơ hội can thiệp tạo khoảnh khắc hài hước/jackpot.
- **Moment-to-moment (15–60s):** thấy cá mới, auto-worker hành động, tap để rút ngắn giao tranh/đặt hàng.
- **Short loop (2–6 phút):** gỡ điểm nghẽn, hoàn thành 2–3 objective, nhận upgrade/visual unlock.
- **Medium loop (15–35 phút):** 6 stage, Boss, Expedition Ship, World mới.
- **Long loop (nhiều phiên):** Bestiary/Aquarium, Trophy, Relics, World Mastery, Prestige tùy chọn.
- **Luật Hybrid:** không có thao tác active bắt buộc để đạt stage clear; active tạo tối đa khoảng 1.25–1.60× tốc độ hoàn thành mục tiêu trong điều kiện điển hình (chỉ là mục tiêu test).
- **Không paywall, không quảng cáo bắt buộc, không yêu cầu online hoặc đăng nhập để vượt 30 stage.**

## 2. CÁC HỆ THỐNG GỐC GIỮ NGUYÊN VÀ PHẦN ĐỀ XUẤT

**Giữ nguyên theo spec nguồn:** `FishermanWorker`, `LiveFish`, `FishWranglerWorker`, `LogisticsTransporterWorker`, `MarketTrendManager` (chu kỳ 120s; bonus x1.4–1.85), `FishingFrenzyManager` (mỗi cá +6%, đủ 100% sau ~17 cá; 20s buff; 2× fishing, 1.5× Wrangler, 2× giá bán), `ContractManager`, `AquariumManager`, `BestiaryManager`, `WorldManager`, `WorldGimmickManager`, `QuestManager`.
**Giữ nguyên công thức nguồn:** `P_final = floor(base × tier × size × quality × trend × upgrade × frenzy × relic)`; các tỷ lệ cỡ cá 75/18/7 và quality 80/14/5/1; ba nhóm upgrade `FishermanTeam`, `WranglerTeam`, `FishValue` với `Cost = round(BaseCost × 1.6^level)` và cap 10 level/World.
**Giữ nguyên World gimmick:** W2 Giant Clam → 3 Golden Pearls → x3 sale trong 20s; W3 Smoker Grill → grilled fish x4; W4 Blizzard → 50% slow, phá băng nhận Gold Shower + x2 fishing; W5 Caravan → 3 Sacred Relics (+30% speed, +50% price, Genie Lamp).
**Đề xuất mới:** 30 stage, `StageProgressionManager`, typed objectives + rewards, deterministic tutorial/rare pity, behavior patterns, Boss phases, Combo tùy chọn, VIP orders có lựa chọn, production queues, Event auto fallback, stage-based analytics, offline earnings + Endgame Mastery.

## 3. LUẬT STAGE / WORLD VÀ SOFTLOCK PROTECTION

1. 5 World × 6 stage, `stage_01` tới `stage_30`; không reset công trình/level nâng cấp khi sang stage tiếp theo trong cùng World.
2. `WorldManager` giữ logic du hành. Khởi hành World mới chỉ sau Boss cuối World **và** ngưỡng Harbor Fame đã được thiết kế để chắc chắn đạt qua main quest. Thiết kế prototype: thưởng +1 Fame khi first clear mỗi stage; ngưỡng lần lượt 6/12/18/24 để du hành, cần mapping sang cơ chế Fame hiện hữu (nguồn không cho ngưỡng số).
3. Tiền Gold có thể giữ xuyên World, còn ba trụ upgrade có level độc lập theo `world_id` (phù hợp cap 10/world trong spec). Cost scale theo World; không reset Gold cưỡng bức.
4. Goal là AND giữa các mục tiêu chính (tối đa 2–3) trong mỗi stage. Optional challenges không được ngăn qua stage.
5. Goal dạng `upgrade_reached` kiểm tra `>= target level` ở thời điểm stage bắt đầu và khi upgrade thay đổi; không yêu cầu mua lại nếu đã đạt.
6. Những mục tiêu cần loài mới, trend hoặc event có đảm bảo sinh (deterministic scheduling / pity). Không dùng quality `Rainbow` (1%) và `Colossal` (7%) làm main goal.
7. Nếu Meter Frenzy đã đầy trước Stage 5, giữ trạng thái 100% tới khi tutorial unlock; nếu quên tap, tự kích sau 30s. Nếu người chơi offline, xử lý sự kiện sau khi load chứ không âm thầm tiêu phí charge.
8. Mọi gimmick tap có fallback auto chậm hơn: tự lấy pearl, tự xử lý smoker, tự phá iceberg, tự mở Genie; AI Wrangler có thể giải mọi Boss.
9. Check boss unlock có condition duy nhất: main goals trước đó completed; optional order không khóa World; khi boss đạt 0HP, phát sự kiện một lần bằng idempotency key.
10. Nếu staging objective gắn event không thể truy hồi, sử dụng dữ liệu state/accumulator (`max`/`cumulative`) khi stage được kích hoạt, tránh kẹt do event đã xảy ra trước đó.

## 4. STAGE DETAILS — 30 STAGES

### WORLD 1: Temperate Bay — Stage 1–6
**Trọng tâm:** Tân thủ, vận hành cảng và Shark Boss. **Gimmick:** Pirate bounty / Shark. **Boss:** Apex Blacktip Shark.
**Species pool đề xuất:** Sardine, Blue Snapper, Hermit Crab, Blacktip Shark. Mỗi cá phải có `FishChainData` (HP, spawnWeight, tier) và dữ liệu behavior tách riêng.

#### Stage 01 — Mẻ cá đầu tiên  
**Pacing:** 1 phút mục tiêu · **Baseline income (chỉ để tune):** ~120 Gold/phút · **Entry:** world_start.
**Bắt buộc hoàn thành:** Câu cá ×1; Hạ cá ×1; Bán cá ×1.
**Gameplay / cơ chế:** Tutorial câu -> cá nhảy lên sàn -> chạm để đánh -> bán; góc camera cố định vào hành động.
**Cá / sự kiện:** Sardine (normal hop).
**Active vs Idle:** Player tap 1–3 lần gây hit-stop 60 ms; auto trợ giúp nếu không tap sau 15 giây.
**Upgrade / kinh tế:** Khởi đầu Fisherman Lv0; cho miễn phí Wrangler tutorial tối thiểu đủ auto-kill.
**Reward / unlock:** Tặng 1 nâng cấp Fisherman cơ bản; mở Stage 2.
**Chống softlock và cảm xúc:** Kết thúc phải thấy vòng Gold đầu tiên trong <= 45 giây.

#### Stage 02 — Cầu cảng náo loạn  
**Pacing:** 1.75 phút mục tiêu · **Baseline income (chỉ để tune):** ~164 Gold/phút · **Entry:** stage_01.
**Bắt buộc hoàn thành:** Hạ cá ×4; Thuê nhân sự ×1 (role=wrangler).
**Gameplay / cơ chế:** Cá nhảy làm Wrangler choáng 4 giây; giới thiệu tự đánh và tuyển Worker.
**Cá / sự kiện:** Blue Snapper.
**Active vs Idle:** Tap con cá chuẩn bị húc để xử lý nhanh hơn, không cần phản xạ bắt buộc.
**Upgrade / kinh tế:** Gợi ý WranglerTeam Lv1 / hiển thị công dụng DPS.
**Reward / unlock:** Mở Fish Wrangler chính thức và nhân sự tự động.
**Chống softlock và cảm xúc:** Sự kiện hất văng có hiệu ứng hài, không gây game-over.

#### Stage 03 — Xe cá đầu tiên  
**Pacing:** 2.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~225 Gold/phút · **Entry:** stage_02.
**Bắt buộc hoàn thành:** Thuê nhân sự ×1 (role=transporter); Vận chuyển hàng ×6.
**Gameplay / cơ chế:** Thùng cá chật, xe đẩy capacity mặc định 3, người chơi xử lý bottleneck đầu tiên.
**Cá / sự kiện:** Hermit Crab (vỏ cứng hình ảnh, không áp giáp gameplay ở W1).
**Active vs Idle:** Chạm vào xe để tăng tốc ngắn tùy chọn; auto logistics vẫn hoàn tất.
**Upgrade / kinh tế:** Gợi ý nâng Fisherman hoặc thuê transporter; thuê 1 xe được trợ giá.
**Reward / unlock:** Mở Logistics, kệ Market và Bestiary sơ cấp.
**Chống softlock và cảm xúc:** Khi tồn kho quá cao, tạm giảm nhịp spawn để tránh backlog vô hạn.

#### Stage 04 — Ngày hội chợ cá  
**Pacing:** 3 phút mục tiêu · **Baseline income (chỉ để tune):** ~309 Gold/phút · **Entry:** stage_03.
**Bắt buộc hoàn thành:** Bán cá ×8; Hoàn tất đơn hàng ×1.
**Gameplay / cơ chế:** Mở Casual Shopper và một đơn hàng dễ, VIP ghé thoáng qua; giới thiệu Harbor Fame.
**Cá / sự kiện:** Sardine, Blue Snapper.
**Active vs Idle:** Chọn nhận đơn, không cần kiếm loài hiếm ngẫu nhiên.
**Upgrade / kinh tế:** Gợi ý FishValue Lv2 hoặc nâng kệ theo cấu hình.
**Reward / unlock:** Mở Contract UI, Fame, Market Trend 120 giây.
**Chống softlock và cảm xúc:** Guaranteed contract dùng cá phổ biến, có đổi đơn miễn phí.

#### Stage 05 — Cơn sốt đánh bắt  
**Pacing:** 3.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~423 Gold/phút · **Entry:** stage_04.
**Bắt buộc hoàn thành:** Hạ cá ×8; Kích hoạt Fishing Frenzy ×1.
**Gameplay / cơ chế:** Mở Frenzy tích 6% mỗi cá bị hạ, khoảng 17 cá; thanh được tích trước Stage 5.
**Cá / sự kiện:** Blue Snapper, Hermit Crab.
**Active vs Idle:** Khi đạt 100%, bấm kích hoạt vào thời điểm thích hợp; auto-trigger sau 30 giây nếu không chạm.
**Upgrade / kinh tế:** Nhắc nâng Wrangler/Fisherman theo bottleneck thực tế.
**Reward / unlock:** Mở Frenzy (20 s, 2x tốc câu, 1.5x Wrangler, 2x bán).
**Chống softlock và cảm xúc:** Nếu meter đã đầy từ stage trước, sẵn sàng kích hoạt tức thì.

#### Stage 06 — Shark đại náo  
**Pacing:** 5 phút mục tiêu · **Baseline income (chỉ để tune):** ~579 Gold/phút · **Entry:** stage_05.
**Bắt buộc hoàn thành:** Đánh bại Boss ×1 (boss_id=blacktip_shark); Hoàn tất đơn hàng ×1.
**Gameplay / cơ chế:** 2 phase: nhảy đập cầu cảng (100–60% HP), lao về vùng cảnh báo (60–0%).
**Cá / sự kiện:** Apex Blacktip Shark.
**Active vs Idle:** Tap weak spot đúng nhịp có stun; Wrangler auto luôn thắng trong thời hạn mục tiêu.
**Upgrade / kinh tế:** Đề xuất Fisherman/ Wranglers/FishValue 3/4/3, không là khóa cứng.
**Reward / unlock:** Trophy Shark + Expedition Ship; mở World 2.
**Chống softlock và cảm xúc:** Phần thưởng Boss không phụ thuộc cá chất lượng Rainbow.

### WORLD 2: Tropical Coral Atoll — Stage 7–12
**Trọng tâm:** Khám phá, tương tác và ngọc trai. **Gimmick:** Giant Clam: 3 Golden Pearls -> Pearl Frenzy x3 trong 20 giây. **Boss:** Colossal Manta Ray.
**Species pool đề xuất:** Coral Snapper, Pufferfish, Electric Eel, Coral Ray, Colossal Manta Ray. Mỗi cá phải có `FishChainData` (HP, spawnWeight, tier) và dữ liệu behavior tách riêng.

#### Stage 07 — Biển san hô đầu tiên  
**Pacing:** 2.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~2,400 Gold/phút · **Entry:** world_start.
**Bắt buộc hoàn thành:** Câu cá ×6; Khám phá loài cá ×1.
**Gameplay / cơ chế:** World mới, giới thiệu Pufferfish dưới dạng silhouette và Bestiary mới.
**Cá / sự kiện:** Coral Snapper, Pufferfish.
**Active vs Idle:** Tap để thu thập nhanh; tự động nhận loài mới theo spawn được đảm bảo.
**Upgrade / kinh tế:** Nâng FishermanTeam của World 2 (reset level địa phương, giữ collection).
**Reward / unlock:** Mở Coral Collection tab trong Bestiary và góc Coral Atoll.
**Chống softlock và cảm xúc:** Loài mới có guarantee ở tối đa catch thứ 6.

#### Stage 08 — Cá nóc nổi giận  
**Pacing:** 3 phút mục tiêu · **Baseline income (chỉ để tune):** ~3,288 Gold/phút · **Entry:** stage_07.
**Bắt buộc hoàn thành:** Hạ cá chỉ định ×3 (species_id=pufferfish); Câu cá ×8.
**Gameplay / cơ chế:** Pufferfish phồng người 2 giây, phản đòn nếu bị đánh lúc phòng thủ; AI Wrangler biết tạm đợi.
**Cá / sự kiện:** Pufferfish.
**Active vs Idle:** Người chơi đánh lúc hết phòng thủ để kích hoạt break + thưởng Gold nhỏ.
**Upgrade / kinh tế:** Gợi ý WranglerTeam Lv2 và animation trang bị mới.
**Reward / unlock:** Mở Fish Behavior: Defensive/Telegraph.
**Chống softlock và cảm xúc:** Nếu người chơi idle thì Wrangler dừng đánh lúc guard, không bị softlock.

#### Stage 09 — Đội xe đảo nhiệt đới  
**Pacing:** 3.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~4,505 Gold/phút · **Entry:** stage_08.
**Bắt buộc hoàn thành:** Vận chuyển hàng ×10; Đạt cấp nâng cấp ×2 (pillar=FishValue).
**Gameplay / cơ chế:** Vận chuyển đông hơn, kệ chứa nhiều mặt hàng; chỉ ra tắc nghẽn do cầu cảng/quầy.
**Cá / sự kiện:** Coral Snapper, Electric Eel.
**Active vs Idle:** Người chơi chọn nâng đúng nút cổ chai để tăng throughput.
**Upgrade / kinh tế:** FishValue Lv2; nâng Transporter capacity bằng unlock phụ riêng.
**Reward / unlock:** Mở capacity improvement và KPI throughput.
**Chống softlock và cảm xúc:** UpgradeReached đọc level hiện tại, không bắt mua lại nếu đã đạt.

#### Stage 10 — Bí mật của Giant Clam  
**Pacing:** 4 phút mục tiêu · **Baseline income (chỉ để tune):** ~6,171 Gold/phút · **Entry:** stage_09.
**Bắt buộc hoàn thành:** Thu thập Golden Pearl ×3; Kích hoạt Pearl Frenzy ×1.
**Gameplay / cơ chế:** Giant Clam định kỳ mở; 3 Golden Pearls kích Pearl Frenzy x3 giá bán 20 giây.
**Cá / sự kiện:** Giant Clam (environment gimmick).
**Active vs Idle:** Tap khi hà mở để lấy ngọc nhanh; nếu idle có auto-harvest chậm/khả năng thu thập thụ động.
**Upgrade / kinh tế:** Gợi ý FishValue Lv3 để thấy giá trị sự kiện.
**Reward / unlock:** Mở Pearl Collection UI và Pearl Frenzy.
**Chống softlock và cảm xúc:** Cam mở theo lịch, không chỉ RNG; không mất ngọc vì nghỉ chơi.

#### Stage 11 — Phiên chợ san hô  
**Pacing:** 4.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~8,455 Gold/phút · **Entry:** stage_10.
**Bắt buộc hoàn thành:** Bán cá đúng Market Trend ×5; Hoàn tất đơn hàng ×2.
**Gameplay / cơ chế:** Thời điểm Market Trend + Pearl Frenzy tạo jackpot; ra hai đơn ngắn khác nhau.
**Cá / sự kiện:** Electric Eel, Coral Ray.
**Active vs Idle:** Chọn giữ hàng phổ biến để bán đúng trend hoặc bán luôn; khác biệt lợi nhuận nhưng không chặn tiến trình.
**Upgrade / kinh tế:** Gợi ý FishValue và đường vận chuyển; thêm 1 quầy.
**Reward / unlock:** Mở đơn hàng chọn lựa, Aquarium placement preview.
**Chống softlock và cảm xúc:** Nếu không có trend khớp, sự kiện phù hợp bắt buộc xuất hiện sau 1 chu kỳ.

#### Stage 12 — Manta của vực san hô  
**Pacing:** 6 phút mục tiêu · **Baseline income (chỉ để tune):** ~11,583 Gold/phút · **Entry:** stage_11.
**Bắt buộc hoàn thành:** Đánh bại Boss ×1 (boss_id=colossal_manta); Đưa cá vào Aquarium ×1.
**Gameplay / cơ chế:** Boss Manta: phase 1 quạt nước hất Worker, phase 2 che chắn bằng bọt; tap bọt phá nhanh.
**Cá / sự kiện:** Colossal Manta Ray.
**Active vs Idle:** Active break shield nhanh; auto Wrangler vẫn phá được.
**Upgrade / kinh tế:** Gợi ý WranglerTeam 4/ Fisherman 3/ FishValue 4.
**Reward / unlock:** Trophy Manta vào Aquarium; mở World 3.
**Chống softlock và cảm xúc:** Aquarium slot miễn phí được cấp ngay sau boss.

### WORLD 3: Autumn Smoker Haven — Stage 13–18
**Trọng tâm:** Chế biến, đơn hàng, quyết định kinh tế. **Gimmick:** Autumn Smoker Grill: cá hun khói x4. **Boss:** Smoker Kraken.
**Species pool đề xuất:** Autumn Salmon, Armored Crab, Ember Eel, Ink Octopus, Smoker Kraken. Mỗi cá phải có `FishChainData` (HP, spawnWeight, tier) và dữ liệu behavior tách riêng.

#### Stage 13 — Mùa cá hồi  
**Pacing:** 3 phút mục tiêu · **Baseline income (chỉ để tune):** ~48,000 Gold/phút · **Entry:** world_start.
**Bắt buộc hoàn thành:** Câu cá ×8; Bán cá ×8.
**Gameplay / cơ chế:** Giới thiệu thị trường mùa thu, cá hồi mới và nguyên liệu cho chế biến.
**Cá / sự kiện:** Autumn Salmon, Armored Crab.
**Active vs Idle:** Chủ động ưu tiên loài bán được giá theo trend.
**Upgrade / kinh tế:** Gợi ý FishermanTeam và FishValue W3.
**Reward / unlock:** Mở khu Smoker bị khóa chờ Stage 14.
**Chống softlock và cảm xúc:** Bảo đảm luồng bán cá thô hoạt động trước khi mở bếp.

#### Stage 14 — Lò hun khói bừng cháy  
**Pacing:** 3.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~65,760 Gold/phút · **Entry:** stage_13.
**Bắt buộc hoàn thành:** Nấu mẻ Smoker ×3; Hoàn tất đơn hàng ×1.
**Gameplay / cơ chế:** Smoker Grill hiện có bán x4; player tap quạt lửa nhanh, auto bếp ở 50–60% tốc thao tác.
**Cá / sự kiện:** Autumn Salmon.
**Active vs Idle:** Tap để rút thời gian nấu; active không phải điều kiện duy nhất.
**Upgrade / kinh tế:** Mở Smoker Lv1; không thêm trụ nâng cấp chính thứ tư.
**Reward / unlock:** Mở Smoker + ba công thức đơn giản theo data.
**Chống softlock và cảm xúc:** Nhân vật không ngừng hoạt động nếu thiếu thao tác.

#### Stage 15 — Dây chuyền chế biến  
**Pacing:** 4 phút mục tiêu · **Baseline income (chỉ để tune):** ~90,091 Gold/phút · **Entry:** stage_14.
**Bắt buộc hoàn thành:** Nấu mẻ Smoker ×6; Vận chuyển hàng ×12.
**Gameplay / cơ chế:** Một hàng chờ cá tươi -> bếp -> quầy; bếp có slot 2, nâng công suất dạng milestone.
**Cá / sự kiện:** Autumn Salmon, Armored Crab.
**Active vs Idle:** Chọn phân bổ cá bán thô hay hun khói; auto chọn theo rule nếu idle.
**Upgrade / kinh tế:** Smoker Queue Capacity unlock phụ; WranglerTeam tăng tốc chuyển cá.
**Reward / unlock:** Mở Auto-Route cho Logistics / Recipe Queue.
**Chống softlock và cảm xúc:** Không tạo inventory deadlock khi tất cả cá vào bếp đang đầy.

#### Stage 16 — Khách VIP ghé thăm  
**Pacing:** 4 phút mục tiêu · **Baseline income (chỉ để tune):** ~123,425 Gold/phút · **Entry:** stage_15.
**Bắt buộc hoàn thành:** Hoàn thành đơn VIP ×2; Bán cá ×12.
**Gameplay / cơ chế:** VIP yêu cầu một mẻ cá hun khói/loài phổ thông; lợi nhuận hơn nhưng mất thời gian.
**Cá / sự kiện:** Autumn Salmon, Ember Eel.
**Active vs Idle:** Chọn đơn VIP trong 2 lựa chọn; mọi đơn có tùy chọn refresh miễn phí.
**Upgrade / kinh tế:** Gợi ý FishValue 4 và năng suất Smoker.
**Reward / unlock:** Mở VIP Priority Order và cosmetic cho quầy.
**Chống softlock và cảm xúc:** Không yêu cầu Rainbow hoặc Colossal trong objective chính.

#### Stage 17 — Combo khói lửa  
**Pacing:** 5 phút mục tiêu · **Baseline income (chỉ để tune):** ~169,092 Gold/phút · **Entry:** stage_16.
**Bắt buộc hoàn thành:** Nấu mẻ Smoker ×8; Kích hoạt Fishing Frenzy ×1.
**Gameplay / cơ chế:** Tận dụng Frenzy + Smoker x4 và các đơn lớn, chuẩn bị Boss.
**Cá / sự kiện:** Ember Eel, Ink Octopus.
**Active vs Idle:** Combo đánh cá + chốt đơn đúng trend cho hiệu ứng thưởng, không bắt buộc combo.
**Upgrade / kinh tế:** Gợi ý cân bằng throughput giữa fishing/ wrangling/ kitchen.
**Reward / unlock:** Mở tùy chọn gear Stun Hammer (nếu nằm trong scope).
**Chống softlock và cảm xúc:** Nếu income spike quá lớn, giới hạn tần suất sự kiện thay vì âm thầm sửa giá.

#### Stage 18 — Kraken trong khói  
**Pacing:** 6 phút mục tiêu · **Baseline income (chỉ để tune):** ~231,656 Gold/phút · **Entry:** stage_17.
**Bắt buộc hoàn thành:** Đánh bại Boss ×1 (boss_id=smoker_kraken); Hoàn tất đơn hàng ×1.
**Gameplay / cơ chế:** Kraken phase 1 xúc tu chắn đường vận chuyển; phase 2 mực đen che một phần dock.
**Cá / sự kiện:** Smoker Kraken (đề xuất mới).
**Active vs Idle:** Tap phá mực và điểm yếu giúp tăng tốc; fallback tự động sau timer.
**Upgrade / kinh tế:** Gợi ý Wrangler 5 và nâng Smoker Queue.
**Reward / unlock:** Boss Trophy + mở World 4.
**Chống softlock và cảm xúc:** Các đường đi không bao giờ bị chặn vĩnh viễn.

### WORLD 4: Arctic Frostfjord — Stage 19–24
**Trọng tâm:** Thời tiết, điểm nghẽn và xử lý rủi ro. **Gimmick:** Blizzard: -50% tốc câu; phá Treasure Iceberg -> x2 tốc câu. **Boss:** Glacier Narwhal.
**Species pool đề xuất:** Arctic Cod, Snow Crab, Frost Fish, Ice Ray, Glacier Narwhal. Mỗi cá phải có `FishChainData` (HP, spawnWeight, tier) và dữ liệu behavior tách riêng.

#### Stage 19 — Khởi hành tới vùng băng  
**Pacing:** 3 phút mục tiêu · **Baseline income (chỉ để tune):** ~960,000 Gold/phút · **Entry:** world_start.
**Bắt buộc hoàn thành:** Câu cá ×8; Bán cá ×8.
**Gameplay / cơ chế:** Dock tuyết phủ, giới thiệu môi trường lạnh, cá Arctic Cod.
**Cá / sự kiện:** Arctic Cod, Snow Crab.
**Active vs Idle:** Nâng cấp cảng mới, nhìn thấy công nhân thích nghi với mùa đông.
**Upgrade / kinh tế:** Gợi ý Fisherman W4 Lv1/Lv2.
**Reward / unlock:** Mở thermal decor và hệ thống thời tiết (chưa gây phạt ở stage đầu).
**Chống softlock và cảm xúc:** Không có blizzard bất ngờ trong 30–45 giây đầu W4.

#### Stage 20 — Cơn bão tuyết đầu tiên  
**Pacing:** 4 phút mục tiêu · **Baseline income (chỉ để tune):** ~1,315,200 Gold/phút · **Entry:** stage_19.
**Bắt buộc hoàn thành:** Phá Treasure Iceberg ×1; Vượt qua Blizzard ×1.
**Gameplay / cơ chế:** Blizzard chậm 50% tốc câu; Treasure Iceberg được tạo theo lịch, phá băng cho Gold + x2 tốc câu.
**Cá / sự kiện:** Treasure Iceberg (environment gimmick).
**Active vs Idle:** Tap liên tục để phá nhanh; Worker tự đập băng theo chu kỳ nếu người chơi không chạm.
**Upgrade / kinh tế:** Mở cold resistance perk nhỏ ngoài 3 trụ chính.
**Reward / unlock:** Mở Blizzard Event + Icebreaker.
**Chống softlock và cảm xúc:** Blizzard có hard timeout và buff recovery; không thể stall vô hạn.

#### Stage 21 — Cua tuyết giáp băng  
**Pacing:** 4 phút mục tiêu · **Baseline income (chỉ để tune):** ~1,801,824 Gold/phút · **Entry:** stage_20.
**Bắt buộc hoàn thành:** Hạ cá chỉ định ×5 (species_id=snow_crab); Đạt cấp nâng cấp ×3 (pillar=WranglerTeam).
**Gameplay / cơ chế:** Cua tuyết có guard phase/animation vỏ băng; Wrangler auto chọn điểm yếu sau telegraph.
**Cá / sự kiện:** Snow Crab.
**Active vs Idle:** Tap lúc vỡ băng được combo break bonus.
**Upgrade / kinh tế:** WranglerTeam Lv3 thay vũ khí / VFX.
**Reward / unlock:** Mở Ice-Break equipment visual.
**Chống softlock và cảm xúc:** Không yêu cầu trang bị rơi ngẫu nhiên.

#### Stage 22 — Tối ưu giữa bão  
**Pacing:** 4.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~2,468,499 Gold/phút · **Entry:** stage_21.
**Bắt buộc hoàn thành:** Vận chuyển hàng ×15; Hoàn tất đơn hàng ×2.
**Gameplay / cơ chế:** Bão có thể giảm tốc câu nhưng không làm giảm tốc độ mọi công đoạn; test cân bằng bottleneck.
**Cá / sự kiện:** Arctic Cod, Ice Ray.
**Active vs Idle:** Người chơi chọn nâng khâu đang nghẽn để giữ cung ứng.
**Upgrade / kinh tế:** Gợi ý Logistics capacity và FishValue.
**Reward / unlock:** Mở Warehouse/Route status widget.
**Chống softlock và cảm xúc:** Mọi đơn có thời gian đủ dài; tránh race giữa bão và order timer.

#### Stage 23 — Kho báu băng trôi  
**Pacing:** 5.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~3,381,843 Gold/phút · **Entry:** stage_22.
**Bắt buộc hoàn thành:** Phá Treasure Iceberg ×2; Bán cá đúng Market Trend ×4.
**Gameplay / cơ chế:** Hai lần phá băng có lịch, có thể tận dụng Gold Shower với Market Trend.
**Cá / sự kiện:** Frost Fish.
**Active vs Idle:** Canh event để tối ưu lợi nhuận, tự động vẫn qua stage.
**Upgrade / kinh tế:** Gợi ý FishermanTeam + FishValue.
**Reward / unlock:** Mở Trophy Iceberg và aquarium decor.
**Chống softlock và cảm xúc:** Nếu người chơi tắt máy, event còn tồn khi mở lại một khoảng grace.

#### Stage 24 — Narwhal băng giá  
**Pacing:** 7 phút mục tiêu · **Baseline income (chỉ để tune):** ~4,633,126 Gold/phút · **Entry:** stage_23.
**Bắt buộc hoàn thành:** Đánh bại Boss ×1 (boss_id=glacier_narwhal); Đưa cá vào Aquarium ×1.
**Gameplay / cơ chế:** Narwhal phase 1 húc theo vùng cảnh báo; phase 2 gây Blizzard + triệu hồi 2 Ice Fish.
**Cá / sự kiện:** Glacier Narwhal (đề xuất mới).
**Active vs Idle:** Ngắt đòn bằng tap weakspot; Wrangler tự xử lý adds.
**Upgrade / kinh tế:** Gợi ý WranglerTeam 5 và hồi phục tốc câu.
**Reward / unlock:** Trophy Narwhal + mở World 5.
**Chống softlock và cảm xúc:** Không spawn quá cap active LiveFish trên mobile.

### WORLD 5: Desert Oasis Harbor — Stage 25–30
**Trọng tâm:** Mastery, Caravan và 3 Sacred Relics. **Gimmick:** Caravan shipments -> Sacred Relics Speed, Price, Genie Lamp. **Boss:** Oasis Leviathan.
**Species pool đề xuất:** Oasis Carp, Sand Ray, Golden Catfish, Mirage Eel, Oasis Leviathan. Mỗi cá phải có `FishChainData` (HP, spawnWeight, tier) và dữ liệu behavior tách riêng.

#### Stage 25 — Đến cảng ốc đảo  
**Pacing:** 3.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~19,200,000 Gold/phút · **Entry:** world_start.
**Bắt buộc hoàn thành:** Câu cá ×10; Bán cá ×10.
**Gameplay / cơ chế:** Giới thiệu cá Oasis Carp, caravan đi qua nền xa; tạo mục tiêu thương mại mới.
**Cá / sự kiện:** Oasis Carp, Sand Ray.
**Active vs Idle:** Đầu tư dàn Worker và mở hệ thống bán tự động.
**Upgrade / kinh tế:** Gợi ý FishermanTeam/FishValue W5.
**Reward / unlock:** Mở Oasis Harbor và Caravan board preview.
**Chống softlock và cảm xúc:** Cho bán cá thông thường trước khi đơn caravan xuất hiện.

#### Stage 26 — Chuyến hàng lạc đà  
**Pacing:** 4.5 phút mục tiêu · **Baseline income (chỉ để tune):** ~26,304,000 Gold/phút · **Entry:** stage_25.
**Bắt buộc hoàn thành:** Hoàn thành chuyến Caravan ×1; Vận chuyển hàng ×12.
**Gameplay / cơ chế:** Caravan yêu cầu shipment đủ lượng cá phổ thông, luôn có phương án không cần cá hiếm.
**Cá / sự kiện:** Oasis Carp, Sand Ray.
**Active vs Idle:** Tự quyết giữ hàng gửi caravan hay bán ngay; auto reserve stock nếu có đơn được chấp nhận.
**Upgrade / kinh tế:** Gợi ý tăng Logistics capacity.
**Reward / unlock:** Mở Caravan + quota progress.
**Chống softlock và cảm xúc:** Quota nhận được sau 2–3 phút chơi bình thường; không yêu cầu timer thực.

#### Stage 27 — Di vật tốc độ  
**Pacing:** 5 phút mục tiêu · **Baseline income (chỉ để tune):** ~36,036,480 Gold/phút · **Entry:** stage_26.
**Bắt buộc hoàn thành:** Hoàn thành chuyến Caravan ×2; Mở Sacred Relic ×1 (relic_id=speed_relic).
**Gameplay / cơ chế:** Tổng shipments tích lũy qua stage >= 3; nhận Sacred Relic #1 +30% speed.
**Cá / sự kiện:** Golden Catfish.
**Active vs Idle:** Gom shipment theo bộ, mở chiếc relic đầu tiên.
**Upgrade / kinh tế:** Relic tự gắn, không tốn Gold sau khi mở.
**Reward / unlock:** Mở Relic #1 (Speed +30%).
**Chống softlock và cảm xúc:** Nếu người chơi đã làm đủ trước stage, objective đọc cumulative count.

#### Stage 28 — Di vật thịnh vượng  
**Pacing:** 5 phút mục tiêu · **Baseline income (chỉ để tune):** ~49,369,978 Gold/phút · **Entry:** stage_27.
**Bắt buộc hoàn thành:** Hoàn thành chuyến Caravan ×2; Mở Sacred Relic ×1 (relic_id=price_relic).
**Gameplay / cơ chế:** Tổng shipments >= 5; nhận Sacred Relic #2 +50% price.
**Cá / sự kiện:** Mirage Eel.
**Active vs Idle:** Chọn order có hiệu quả cao, tối ưu chiến lược giữ hàng.
**Upgrade / kinh tế:** Gợi ý FishValue và VIP order.
**Reward / unlock:** Mở Relic #2 (Price +50%).
**Chống softlock và cảm xúc:** Relic không buộc chọn giữa các lựa chọn không thể đổi.

#### Stage 29 — Thần đèn ốc đảo  
**Pacing:** 6 phút mục tiêu · **Baseline income (chỉ để tune):** ~67,636,869 Gold/phút · **Entry:** stage_28.
**Bắt buộc hoàn thành:** Hoàn thành chuyến Caravan ×3; Kích hoạt Genie Lamp ×1.
**Gameplay / cơ chế:** Tổng shipments >= 8; Sacred Relic #3 Genie Lamp (buff có cooldown).
**Cá / sự kiện:** Golden Catfish, Mirage Eel.
**Active vs Idle:** Chủ động kích Genie cho burst; auto kích sau một khoảng idle nếu có charge.
**Upgrade / kinh tế:** Gợi ý WranglerTeam / FishValue trước boss.
**Reward / unlock:** Mở Relic #3 (Genie Lamp) và Mastery Board.
**Chống softlock và cảm xúc:** Genie charge đầu tiên cấp sẵn, không đòi RNG.

#### Stage 30 — Leviathan cuối chân trời  
**Pacing:** 8 phút mục tiêu · **Baseline income (chỉ để tune):** ~92,662,511 Gold/phút · **Entry:** stage_29.
**Bắt buộc hoàn thành:** Đánh bại Boss ×1 (boss_id=oasis_leviathan); Hoàn thành chuyến Caravan ×1.
**Gameplay / cơ chế:** Boss 3 phase: cát lốc, gọi Sand Ray, phá giáp yếu điểm; kết hợp các phép đã học.
**Cá / sự kiện:** Oasis Leviathan (đề xuất mới).
**Active vs Idle:** Active phá giáp để đánh nhanh, auto hoàn thành qua timeout phase.
**Upgrade / kinh tế:** Khuyến nghị mỗi trụ 5–6 nhưng không ép mua.
**Reward / unlock:** Trophy, Credits ngắn, Prestige tùy chọn, mở Mastery/Endless Goals.
**Chống softlock và cảm xúc:** Không tự prestige hoặc reset sau thắng boss; yêu cầu xác nhận tự nguyện.

## 5. BOSS DESIGN BIBLE — NĂM BOSS CHÍNH

| Stage | Boss | Phase & Telegraph | Active bonus | Idle guarantee |
|---|---|---|---|---|
| 06 | Apex Blacktip Shark | 100–60% HP: nhảy/đập; 60–0%: charge + vùng đỏ cảnh báo 1.5s | Chạm điểm yếu stun 2s | Auto Wrangler né sau warning, cooldown attack reset |
| 12 | Colossal Manta Ray | Đuôi quạt; dưới 50% tạo Bubble Shield | Tap bọt phá shield nhanh | Shield tự vỡ sau N đòn auto/timer |
| 18 | Smoker Kraken *(mới)* | Xúc tu cản cart; 40% HP phun mực | Tap chỗ mực để mở góc nhìn | Không đóng đường hoàn toàn; mực hết sau timer |
| 24 | Glacier Narwhal *(mới)* | Húc đường thẳng; 40% HP bão + gọi 2 cá con | Weakspot interrupt | Adds cap 2; Wranglers tự ưu tiên adds và boss |
| 30 | Oasis Leviathan *(mới)* | 100–65% Sandstorm; 65–35% summon; 35–0% armor+weakspot | Phá giáp cho burst Gold/Combo | Armor bị phá bởi Wrangler sau tối đa 3 cycle |

**Tham số mục tiêu để playtest (chưa phải HP thật):** regular fish idle TTK 6–15s, active 3–8s; boss idle TTK 75–150s, active 45–100s với build chuẩn; worker KO 4s theo spec. Không tạo DPS check chặn tiến trình. Tổng cá sống đồng thời giới hạn theo thiết bị (ví dụ soft cap 8, hard cap 12 để thử nghiệm), boss adds dùng pool.
**Boss reward:** Unique Trophy (guaranteed) + lần đầu Aquarium specimen hoặc decor + 1.25 phút lợi nhuận tham chiếu + 1 Fame. Boss replay chỉ có thưởng nhỏ để tránh farm vô tận.

## 6. CÁ, HÀNH VI, RARITY VÀ COLLECTION

| Behavior (đề xuất) | Telegraph | Phản ứng player | Fallback AI | World đầu tiên |
|---|---|---|---|---|
| Normal Flopper | Nhảy, chuẩn bị tiếp đất | Tap để hạ nhanh | Auto nhắm trực tiếp | W1 |
| Defensive Puffer | Phồng 2s | Chờ hết guard, tap ngắt | Wrangler dừng tấn công trong guard | W2 |
| Electric Eel | Vệt điện 1.2s | Tap weakspot trước stun | Wrangler ưu tiên tản và hồi phục sau stun | W2 |
| Armored Crab | Vỏ sáng, có armor | Break shell để tăng DPS | AI dùng damage tích lũy phá vỏ | W3–4 |
| Ice Fish / Snow Crab | Phủ băng, giải băng sau vài hit | Hit đúng timing bonus | Auto break trong <=15s | W4 |
| Mirage Eel | Ảo ảnh ngắn | Tap đúng cá gốc | AI target theo `entityId`, không đánh bóng giả | W5 |

**Collection:** Milestone Bestiary nguồn (3 species +15% fishing, 5 species +20% sell price, 7 species +30% Wrangler DPS và +50% tips) được giữ nguyên. Boss specimen nhận chắc chắn; loài thường mới có pity nếu là mục tiêu stage. Không yêu cầu người chơi săn Rainbow 1% để mở nội dung cốt lõi.

## 7. UPGRADE DESIGN VÀ MILESTONE NHÌN THẤY ĐƯỢC

| Pillar gốc | Lv1 | Lv3 | Lv6 | Lv10 |
|---|---|---|---|---|
| `FishermanTeam` | Rút ngắn chờ cá | Cần câu mới + VFX +1 slot sau khi có chỗ | Thêm Worker/đế câu theo config | Dàn cần câu cao cấp |
| `WranglerTeam` | Hạ cá ổn định | Vũ khí mới + hit shockwave | Thêm Wrangler + ngoại hình đặc biệt | Đòn đánh kết hợp/giáp đẹp |
| `FishValue` | Tăng giá bán | Nâng model gian hàng | Trang trí signage + VIP visual | Quầy premium + celebration FX |

**Không thêm trụ nâng cấp chính thứ tư.** Những nâng cấp như Smoker Queue, Cart Capacity, decor dùng milestone/unlock nhỏ, không cạnh tranh dồn dập với 3 trụ và không tạo màn hình 15 nút upgrade.
**Suggestion system:** tính chỉ số bottleneck mỗi 10s: `backlogPier`, `storageFill`, `shelfStockout`, `customerQueue`. Chỉ hiển thị *một* gợi ý chính. Không auto tiêu tiền; đề xuất upgrade nên phù hợp wallet.

## 8. ECONOMY TUNING: CÔNG THỨC VÀ MỐC PHÚT

### 8.1 Giữ gốc và đưa vào cân bằng dữ liệu
Base Cost nguồn W1 = 50, `R_cost = 1.60`, `maxLevel = 10`, giá cá gốc khoảng 10–120, tốc Frenzy và multiplier lấy nguyên từ GDD cũ. Công thức giá bán vẫn nhân các yếu tố Size / Quality / Trend / Upgrade / Frenzy / Relic.
**Giả định bổ sung:** `WorldScale = [1, 20, 400, 8000, 160000]` nhân vào base fish-price, world-specific cost, contract payout. Đây là đề xuất prototype để Gold được giữ xuyên World mà economy mới vẫn có ý nghĩa; các World khác hiện chưa có scale chính thức trong source.
`Cost(world, currentLevel) = round(50 × WorldScale[world] × 1.6 ^ currentLevel)`  
`BaseIncomeTarget(world, stageInWorld) = WorldScale × 120 × 1.37 ^ (stageInWorld - 1) Gold/min`  
`StageClearReward = BaseIncomeTarget × (Boss ? 1.25 : 0.35) phút` (Gold-equivalent, có thể thay bằng cosmetic/slot; trả chỉ lần đầu).
Công thức IncomeTarget chỉ là đường baseline dự kiến cho QA và auto-tuner, **không** bắt buộc người chơi kiếm đủ lượng Gold đó mới qua stage. Đo actual GPM trong game để sửa về gần mục tiêu.
### 8.2 Pacing targets theo World
| World | Stage target (phút) | Tổng dự kiến | WorldScale | Trọng tâm kinh tế |
|---|---|---|---:|---|
| 1 — Temperate Bay | 1 / 1.75 / 2.5 / 3 / 3.5 / 5 | 16.75 phút | ×1 | Vận hành cơ bản |
| 2 — Tropical Coral Atoll | 2.5 / 3 / 3.5 / 4 / 4.5 / 6 | 23.5 phút | ×20 | Ngọc + Trend |
| 3 — Autumn Smoker Haven | 3 / 3.5 / 4 / 4 / 5 / 6 | 25.5 phút | ×400 | Bán thô vs Smoker |
| 4 — Arctic Frostfjord | 3 / 4 / 4 / 4.5 / 5.5 / 7 | 28 phút | ×8,000 | Kiểm soát giảm sản lượng |
| 5 — Desert Oasis Harbor | 3.5 / 4.5 / 5 / 5 / 6 / 8 | 32 phút | ×160,000 | Caravan & Relics |

**Tổng mục tiêu first clear 30 stage:** 125.8 phút ~ 2.1 giờ active play. Đây là P50 playtest giả định, không dự báo retention. Người chơi Idle có thể lâu hơn; tránh ép chờ timer.

### 8.3 Economy policies quan trọng
- **Độ hiếm:** Size 75%/18%/7%; Quality 80%/14%/5%/1% như spec gốc; pity chỉ điều chỉnh xuất hiện loài phục vụ quest, không âm thầm tăng roll Rainbow trừ khi được cân bằng công khai.
- **Multiplier jackpot:** Spec gốc ví dụ 52.8× giá cá thường khi Colossal × Rainbow × Trend × Pearl Frenzy. Giữ cách nhân hiện tại ở prototype, nhưng theo dõi `P99 sale value / median sale value`. Nếu phá economy, có thể dùng stacking group (`StandardFrenzy` và `PearlFrenzy` chọn buff cao hơn); **đó là thay đổi gameplay phải phê duyệt riêng**.
- **Không ép Gold gate:** stage không có tiêu chí cần tích lũy coin lớn, thay bằng hành động có ý nghĩa. Đặt mục tiêu upgrade vừa tầm trong 1–2 phút thu nhập hiện tại.
- **Nguồn và sink:** Gold kiếm từ bán cá, VIP, contracts, tip, aquarium, boss, offline; tiêu cho 3 trụ + capacity/decor. Reward first-clear không được loop farm.
- **Offline earnings (đề xuất):** 35–60% `last_stable_GPM` × `min(absence, 8h)`, loại trừ Boss/Frenzy/Market Trend/Rainbow jackpot; chỉ trả cho sản lượng công nhân đã unlock, không tự hoàn thành nhiệm vụ event.
- **Caravan cumulative shipments:** 1 ở Stage26, >=3 mở Speed Relic Stage27, >=5 mở Price Relic Stage28, >=8 mở Genie Lamp Stage29. Stage objective dùng delta ship và unlock milestone cumulative.

## 9. RETENTION VÀ METAGAME

| Horizon | Mục tiêu chính | Reward | Không được làm |
|---|---|---|---|
| 0–5 phút | Bắt, hạ, bán cá; thuê Worker; thấy cảng chạy | Gold, mở bến và Cart | Không popup dài, không quảng cáo ép |
| 5–20 phút | Frenzy, VIP Order, Shark Boss | Trophy + tàu du hành | Không gate Boss bằng rare RNG |
| Ngày 1–2 | Coral/Pearl, Bestiary, Aquarium | Specimen, trang trí, passive income | Không khóa stage theo ngày |
| Ngày 3–4 | Smoker, gear visual, VIP recipe | Production upgrade, trophy | Không tạo 2–3 hệ thống UI mới mỗi phút |
| Ngày 5–7 | Arctic + Oasis, Relics, đa dạng Boss | Relics + Mastery | Không đòi đăng nhập đủ số ngày |
| Sau Stage 30 | World Mastery objective, Trophy collection, Prestige opt-in | Cosmetic, multiplier dài hạn có kiểm soát | Không reset tự động, không fomo |

**Nhiệm vụ lặp lại có chọn lọc:** daily 3 nhiệm vụ nhỏ từ các cơ chế đã biết + 1 weekly collection. Có thể chơi bình thường không cần check-in. Reward cosmetic/Gold ít; không yêu cầu watch ads.
**Prestige (tùy chọn sau S30):** chỉ khả dụng khi người chơi bấm đồng ý xem preview rõ mất gì/giữ gì. Giữ Bestiary, Trophy, collection, cosmetic; reset kinh tế theo lựa chọn. Chưa đặt hệ số Prestige ở v1 trước playtest.
**Analytics / hypotheses:** event `stage_started`, `stage_objective_progress`, `stage_completed`, `stage_abandoned`, `upgrade_purchased`, `boss_started`, `boss_completed`, `frenzy_activated`, `offline_reward_claimed`, `session_end`. Thu thập stage_id, world_id, elapsed, active_taps, auto_damage_ratio, gold_gpm, queue_backlog, FPS_p95, seed/test cohort; không thu dữ liệu cá nhân không cần thiết.
**Success metrics thử nghiệm:** S1 hoàn thành <= 90s median, World1 completion cohort target >=35–50% là *giả thuyết thiết kế*, không phải industry benchmark; xem funnel actual, D1/D7, thời gian idle/active, session count, vị trí stage bị dừng và test A/B với đủ sample.

## 10. KIẾN TRÚC UNITY NÊN BỔ SUNG

### 10.1 Giữ decoupled manager-worker hiện hữu
Các class theo nguồn `WorldManager`, `GameEvents`, `QuestManager`, `EconomyManager`, `UpgradeManager`, `FishingPierManager`, `WranglerManager`, `MarketTrendManager`, `ContractManager`, `AquariumManager`, `WorldGimmickManager` giữ nguyên vai trò. Mở rộng kiểu adapter/observer thay vì sửa tay 30 file/câu lệnh khổng lồ.
### 10.2 Modules mới (đề xuất)
| Component | Vai trò | Interface/điểm kết nối |
|---|---|---|
| `StageProgressionManager` | State stage, enter/complete, reward once, transition | Subscribe GameEvents; `EnterStage`, `TryComplete` |
| `StageData : ScriptableObject` | goal configs, unlocks, reward, expected minutes, new mechanisms | Addressable/local resources per `world_id` |
| `StageObjectiveData` + `ObjectiveRuntime` | typed event counters; snapshot-safe + idempotent | `IStageObjective`, `ApplyEvent` |
| `StageRewardService` | thưởng coin/visual/unlock đúng một lần | idempotency key `profileId:stageId:rewardVersion` |
| `FishBehaviorData` + `FishBehaviorController` | Normal, Puffer Guard, Electric Telegraph, Armor, Mirage | compose với `LiveFish` — không lồng switch khổng lồ |
| `BossEncounterData` + `BossEncounterController` | Phase configs, spawn/timeline, AI fallback, reward | Bridge `BossFishDefeated` + phase events |
| `GimmickAutoFallback` | autoplay Pearl/Grill/Ice/Genie theo timer | gắn vào `WorldGimmickManager` |
| `ProductionQueueController` | cá sống/thô/hun khói, auto routing, kho giới hạn | `LogisticsManager`, `DisplayShelf` |
| `ContractOfferService` | offer deterministic, safe refresh, guaranteed species | `ContractManager`, `MarketTrendManager` |
| `CollectionProgressionService` | trophy/Bestiary/Aquarium milestones | `BestiaryManager`, `AquariumManager` |
| `OfflineIncomeService` | thu nhập hợp lý, anti clock exploit | `EconomyManager`, save UTC + monotonic policy |
| `BottleneckHintService` | phân tích throughput/queue và gợi ý một upgrade | `UpgradeManager`, `LogisticsManager` |
| `StageTelemetry` | funnel, timer, lỗi runtime, auto-only QA | analytics abstraction injected |
| `StageDebugPanel` (editor only) | nhảy stage, hoàn thành goal, simulate offline, force boss | editor tooling |

### 10.3 Sự kiện typed cần map
Event nguồn: `FishermanCaughtFish`, `FishDefeated`, `BossFishDefeated`, `ItemSold`, `QuestCompleted`, `MarketTrendChanged`, `FrenzyChargeChanged`, `HarborFameChanged`. Cần thêm event đã xác nhận qua code: `WorkerHired`, `FishPayloadTransported`, `ContractFulfilled`, `SmokerBatchFinished`, `PearlCollected`, `IcebergBroken`, `RelicUnlocked`, `AquariumSpeciesAdded`, `WorldGimmickActivated`, `UpgradePurchased`. Nếu repo đã có event tương đương thì dùng adapter, không tạo event trùng.
**Không tham chiếu `GameObject` / `MonoBehaviour` trực tiếp trong JSON stage.** `StageData` chỉ chứa ID và cấu hình; mục tiêu dùng typed enum `GoalType` + filters, validated lúc build.
### 10.4 Sample C# shape — minh họa API, không phải code đã compile
```csharp
public enum GoalType { CatchFish, DefeatFish, SellItem, HireWorker, TransportPayload,
    CompleteOrder, ActivateFrenzy, DefeatBoss, DiscoverSpecies, DefeatSpecies,
    UpgradeReached, CollectPearl, ActivatePearlFrenzy, TrendMatchSale,
    AddAquariumSpecies, GrillBatch, CompleteVipOrder, BreakIceberg, ClearBlizzard,
    CompleteCaravanShipment, UnlockRelic, ActivateGenieLamp }
[Serializable] public struct StageGoalConfig { public GoalType type; public int count;
    public string filterId; public bool cumulative; }
[CreateAssetMenu(menuName="IdleFishing/Stage")] public sealed class StageData : ScriptableObject {
    public string stageId, worldId; public StageGoalConfig[] goals;
    public float targetMinutes; public string[] unlockIds; public RewardDefinition reward; }
```
**Runtime data:** `StageSave { stageId, status, progressByGoalId, rewardClaimed, lastProcessedEventId }`. Mọi lần replay event/restore không được nhận thưởng gấp đôi; event đánh cá dùng entity identifier để loại double defeat/sell.
### 10.5 Save/load, performance & compatibility
- Cần serialize theo `worldId/stageId` + schema version, migration khi thêm stage; khi có save PlayerPrefs cũ phải có migrator trước khi chuyển sang JSON. Sử dụng ghi atomic temp→replace, checksum hoặc integrity check, fallback bản save trước.
- Pool `LiveFish`, hit VFX, popups, boss adds (nguồn đang instantiate/destroy); limit số cá/số worker theo ngân sách thiết bị; giảm tần suất AI query hơn là full Physics.OverlapSphere mỗi FixedUpdate; giữ frame-time profiling trên thiết bị thật.
- Không dùng blocking disk IO trong combat; camera focus không spam, UI milestone/celebration có skip; locale-independent IDs; content load theo World.
- Target hiệu năng *đề xuất* 30 FPS ổn định trên máy mục tiêu, ưu tiên đạt 60 FPS trên máy khá; budget validate bằng Unity Profiler, RenderDoc/Frame Debugger trên thiết bị.

## 11. TEST CASES / ACCEPTANCE CRITERIA

**Functional:** 30 stage sequential; được reload giữa goal; sau reload không mất tiến độ và không double reward; không thể mở World trước khi Boss+Fame; main goal không đòi quality RNG; upgrade đạt cấp trước stage vẫn được công nhận; cap stage 30 giữ endgame mở.
**Idle-only:** chạy bot không tap (chỉ accept upgrade/quest auto fallback) từ S1–S30, tất cả event/Boss/pearl/smoker/ice/Genie tự hoàn tất và không kẹt. Nếu UX muốn bấm bắt buộc chỉ dùng trong tutorial đầu tiên rồi chuyển thành auto.
**Active:** user chạm nhiều không gây multiply damage/exploit; cooldown server-side/logic state; không nhận double-boss-kill; auto đạt 70% DPS còn player tap không tăng vô hạn.
**Economy:** simulate RNG 1,000 seeds và bot automation; check P50/P90 stage time vs target, P99 sale jackpot, total sink/source, max wait to afford suggested upgrade, no quest lost to item sale, tracking trend market rotation.
**Performance:** pool stress 12 fish, 10 workers, 6 tourists, 4 carts, 1 boss; test 30/60fps devices, GC Alloc/spike, physics overlap, NavMesh/crowd, DOTS chưa cần trừ khi profiler chỉ rõ.
**UX:** Stage objective tối đa 3 hàng; camera không rời action quá 2–3s; first fish seen trong 10s; tutorial input accessible; có skip boss intro; text legible; không có forced ads.
**World transitions:** Stage06→07, 12→13, 18→19, 24→25, 30→Mastery qua ship/celebration, đúng save + reload; giữ collection và wallet; world-specific upgrade state chuẩn.

## 12. LỘ TRÌNH TRIỂN KHAI DÀNH CHO CODEX

**Phase 0 — Audit (không thay gameplay):** kiểm tra class/event/ScriptableObject/save thật trong Unity project; báo những khác biệt với GDD nguồn; xác định event nào đã có.
**Phase 1 — Vertical Slice World 1:** stage data pipeline, S1–S6, auto-only boss, min progress UI, first-clear reward và save/load migration. Playtest và đo trước khi mở W2.
**Phase 2 — W2:** behaviors Puffer/Eel, Pearl event có fallback, Manta Boss, Aquarium hook.
**Phase 3 — W3:** Smoker production queue, route rules, VIP offer guarantee, Kraken phases.
**Phase 4 — W4:** weather scheduler, iceberg auto fallback, Arctic behavior, Narwhal Boss.
**Phase 5 — W5:** Caravan aggregate shipment milestones, Sacred Relics/Genie, final Boss, Mastery.
**Phase 6 — Stabilize:** economy simulations, performance profiling, telemetry, localization/accessibility, regression suite và cân bằng thật bằng dữ liệu.
**Definition of Done cho mỗi phase:** tất cả goal có automated test; idle-only hoàn thành được; clear reward chỉ một lần; save/load qua scene/world; no missing references; stable mobile framerate; ảnh/camera/VFX không che HUD.

## 13. PROMPT CODEx KHỞI ĐẦU (COPY/PASTE)

```text
Role: Principal Unity Engineer / Gameplay Architect for an existing Unity URP project named Idle Fishing.
Read GAMEPLAY_ARCHITECTURE_SPEC.md, IdleFishing_30_Stage_GDD.md and IdleFishing_30_Stages_Data.json.
First AUDIT the actual repository; list exact classes, events, ScriptableObjects, save format and deviations from these documents.
Do NOT implement all 30 stages at once. Build Phase 1 vertical slice Stage 01-06 only after the audit.
Implement data-driven StageData ScriptableObjects, StageProgressionManager, typed objective adapters, exact-once rewards, world gate, save migration and an editor stage debug panel.
Reuse existing manager-worker architecture/GameEvents and real project scripts. Avoid duplicating existing QuestManager, WorldManager, or EconomyManager responsibilities.
Provide auto-fallback for active interactions. Stage 06 boss must be winnable without player tapping.
Write EditMode and PlayMode tests: 6 stages, reload, no double reward, Frenzy tutorial state, boss phase/auto-only, world transfer.
Before adding new fish/boss assets, use placeholder prefabs safely and generate a list of artist-facing asset requirements.
Do not change multiplier formulas or persistent economy balances silently; propose parameter changes separately.
Output: implementation plan, files touched, tests, changelog, known trade-offs. Do not create unused abstractions.
```

## 14. CÁC QUYẾT ĐỊNH CẦN XÁC NHẬN QUA PLAYTEST / CODE AUDIT

1. Source chỉ xác nhận gameplay architecture, **không xác nhận** implementation hoàn chỉnh của tất cả manager. Audit repository để tránh design-on-paper.
2. Phân tách `QuestManager` và `StageProgressionManager`: quest có thể làm tutorial/side objectives, Stage là meta clear — quyết định reuse sau code audit.
3. Giant Clam / Grill / Icebreaker vốn thiết kế tap; auto fallback là quyết định mới nhằm đáp ứng Hybrid Idle. Tune tốc auto sau player test.
4. Hệ thống Gold xuyên World vs per-World economy: proposal là giữ wallet xuyên World với ×20 scale, cần thử nghiệm anti-skip.
5. Multipliers Standard Frenzy, Pearl Frenzy, Smoker có stack? Source không định nghĩa rõ mọi tổ hợp. Giữ logic runtime cũ cho prototype, đo economy trước khi đổi.
6. Prestige hiện được nhắc ở class map nhưng loop cụ thể chưa được xác nhận; không tự động reset progression Stage 30.
7. Làm World 1 vertical slice và prototype economy trước khi art/design full 30 stage để giảm chi phí làm lại.

---
**Kết luận:** Stage tạo nhịp, World tạo chủ đề, Automation tạo tăng trưởng, Action tạo khoảnh khắc đáng nhớ. Các mục tiêu đều có fallback và được kiểm chứng bằng telemetry + bot. Đây là bản thiết kế v1 có thể chuyển hóa thành ScriptableObject và PlayMode tests, không phải cam kết KPI thực tế.