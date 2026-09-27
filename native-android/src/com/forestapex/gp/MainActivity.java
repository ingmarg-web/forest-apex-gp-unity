package com.forestapex.gp;

import android.app.Activity;
import android.content.Context;
import android.content.pm.ActivityInfo;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.RectF;
import android.graphics.Shader;
import android.os.Bundle;
import android.view.MotionEvent;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;

import java.util.Locale;

public class MainActivity extends Activity {
    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE);
        immersive();
        setContentView(new ForestView(this));
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) immersive();
    }

    private void immersive() {
        getWindow().getDecorView().setSystemUiVisibility(
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY |
            View.SYSTEM_UI_FLAG_FULLSCREEN |
            View.SYSTEM_UI_FLAG_HIDE_NAVIGATION |
            View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN |
            View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION |
            View.SYSTEM_UI_FLAG_LAYOUT_STABLE
        );
    }

    static final class ForestView extends View {
        static final int MENU = 0;
        static final int RACE = 1;
        static final int RESULTS = 2;

        static final float TRACK_LENGTH = 2650f;
        static final int LAPS = 3;
        static final int AI_COUNT = 5;

        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
        final Path path = new Path();
        final RectF rect = new RectF();

        final String[] carNames = {"APEX R", "VELOCE GT", "RIDGE RS"};
        final String[] trackNames = {"PINE RIDGE", "SUNSET GULCH", "NEON DOWNTOWN"};
        final String[] trackSub = {
            "MOUNTAIN TECHNICAL  •  PRECISION / RHYTHM",
            "DESERT SPEED  •  DRAFTING / HIGH SPEED",
            "URBAN NIGHT  •  BRIDGE / OVERTAKING"
        };

        final int[] carColors = {
            Color.rgb(34, 170, 255),
            Color.rgb(244, 61, 45),
            Color.rgb(247, 184, 25)
        };

        int state = MENU;
        int selectedCar = 0;
        int selectedTrack = 0;

        long lastFrame;
        float speed;
        float lateral;
        float steer;
        boolean gas;
        boolean brake;
        float progress;
        int finalPosition;

        final float[] aiProgress = new float[AI_COUNT];
        final float[] aiBaseSpeed = {178f, 187f, 193f, 181f, 199f};
        final float[] aiLane = {-0.68f, 0.52f, -0.18f, 0.75f, 0.12f};

        int steerPointer = -1;
        int gasPointer = -1;
        int brakePointer = -1;

        float joyCx, joyCy, joyR;
        float gasCx, gasCy, gasR;
        float brakeCx, brakeCy, brakeR;

        ForestView(Context context) {
            super(context);
            setFocusable(true);
            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.NORMAL));
            lastFrame = System.nanoTime();
        }

        @Override
        protected void onDraw(Canvas c) {
            super.onDraw(c);
            long now = System.nanoTime();
            float dt = Math.min(0.033f, Math.max(0.001f, (now - lastFrame) / 1_000_000_000f));
            lastFrame = now;

            if (state == RACE) updateRace(dt);

            if (state == MENU) drawMenu(c);
            else {
                drawRace(c);
                if (state == RESULTS) drawResults(c);
            }

            postInvalidateOnAnimation();
        }

        void updateRace(float dt) {
            float accel = selectedCar == 0 ? 43f : selectedCar == 1 ? 39f : 46f;
            float max = selectedCar == 0 ? 236f : selectedCar == 1 ? 252f : 224f;
            float grip = selectedCar == 2 ? 1.08f : selectedCar == 1 ? 0.92f : 1f;

            if (gas) speed += accel * dt;
            else speed -= (8f + speed * 0.014f) * dt;

            if (brake) speed -= 76f * dt;
            speed = clamp(speed, 0, max);

            float trackPos = progress % TRACK_LENGTH;
            float curve = curveAt(selectedTrack, trackPos);
            float speedFactor = clamp(speed / 185f, 0.12f, 1.3f);

            lateral += steer * (1.25f * grip) * speedFactor * dt;
            lateral -= curve * speedFactor * 0.23f * dt;
            lateral *= (float)Math.pow(0.997, dt * 60f);

            if (Math.abs(lateral) > 1.02f) {
                speed -= 44f * dt;
                lateral = clamp(lateral, -1.25f, 1.25f);
            }

            progress += (speed / 3.6f) * dt;

            for (int i = 0; i < AI_COUNT; i++) {
                float phase = aiProgress[i] % TRACK_LENGTH;
                float cornerPenalty = Math.abs(curveAt(selectedTrack, phase)) * 14f;
                float target = aiBaseSpeed[i] - cornerPenalty;
                aiProgress[i] += (target / 3.6f) * dt;
            }

            if (progress >= TRACK_LENGTH * LAPS) {
                finalPosition = currentPosition();
                state = RESULTS;
                gas = false;
                brake = false;
                steer = 0;
            }
        }

        int currentPosition() {
            int pos = 1;
            for (int i = 0; i < AI_COUNT; i++) {
                if (aiProgress[i] > progress) pos++;
            }
            return pos;
        }

        void startRace() {
            state = RACE;
            speed = 0;
            lateral = 0;
            steer = 0;
            progress = 0;
            gas = false;
            brake = false;

            float[] starts = {34f, 61f, 87f, 18f, 108f};
            for (int i = 0; i < AI_COUNT; i++) aiProgress[i] = starts[i];
        }

        void drawMenu(Canvas c) {
            int w = getWidth();
            int h = getHeight();

            LinearGradient bg = new LinearGradient(0, 0, w, h,
                Color.rgb(7, 13, 22), Color.rgb(19, 33, 48), Shader.TileMode.CLAMP);
            p.setShader(bg);
            c.drawRect(0, 0, w, h, p);
            p.setShader(null);

            p.setColor(Color.WHITE);
            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.BOLD));
            p.setTextSize(h * 0.085f);
            c.drawText("FOREST APEX GP", w * 0.045f, h * 0.115f, p);

            p.setColor(Color.rgb(85, 204, 255));
            p.setTextSize(h * 0.029f);
            c.drawText("UNITY MIGRATION • MOBILE RACING", w * 0.048f, h * 0.163f, p);

            float margin = w * 0.045f;
            float gap = w * 0.025f;
            float top = h * 0.23f;
            float panelH = h * 0.59f;
            float panelW = (w - margin * 2 - gap) * 0.5f;

            drawPanel(c, margin, top, panelW, panelH);
            drawPanel(c, margin + panelW + gap, top, panelW, panelH);

            p.setColor(Color.rgb(178, 205, 224));
            p.setTextSize(h * 0.026f);
            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.BOLD));
            c.drawText("GARAGE", margin + panelW * 0.06f, top + panelH * 0.10f, p);
            c.drawText("EVENT SELECT", margin + panelW + gap + panelW * 0.06f, top + panelH * 0.10f, p);

            p.setColor(Color.WHITE);
            p.setTextAlign(Paint.Align.CENTER);
            p.setTextSize(h * 0.055f);
            c.drawText(carNames[selectedCar], margin + panelW * 0.5f, top + panelH * 0.25f, p);
            c.drawText(trackNames[selectedTrack], margin + panelW + gap + panelW * 0.5f, top + panelH * 0.25f, p);

            drawCarHero(c, margin + panelW * 0.5f, top + panelH * 0.48f, panelW * 0.58f, carColors[selectedCar]);

            p.setColor(Color.rgb(145, 184, 207));
            p.setTextSize(h * 0.0205f);
            c.drawText(trackSub[selectedTrack], margin + panelW + gap + panelW * 0.5f, top + panelH * 0.38f, p);

            drawTrackPreview(c, selectedTrack,
                margin + panelW + gap + panelW * 0.12f,
                top + panelH * 0.45f,
                panelW * 0.76f,
                panelH * 0.30f);

            p.setTextAlign(Paint.Align.LEFT);
            p.setTextSize(h * 0.020f);
            p.setColor(Color.rgb(207, 221, 232));
            float statX = margin + panelW * 0.17f;
            float statY = top + panelH * 0.73f;
            c.drawText("ACCELERATION", statX, statY, p);
            c.drawText("TOP SPEED", statX, statY + h * 0.04f, p);
            c.drawText("HANDLING", statX, statY + h * 0.08f, p);
            drawBar(c, statX + panelW * 0.33f, statY - h * 0.017f, panelW * 0.35f, h * 0.014f, selectedCar == 2 ? .94f : selectedCar == 0 ? .86f : .80f);
            drawBar(c, statX + panelW * 0.33f, statY + h * 0.023f, panelW * 0.35f, h * 0.014f, selectedCar == 1 ? .97f : selectedCar == 0 ? .89f : .82f);
            drawBar(c, statX + panelW * 0.33f, statY + h * 0.063f, panelW * 0.35f, h * 0.014f, selectedCar == 2 ? .96f : selectedCar == 0 ? .90f : .83f);

            drawArrowButton(c, margin + panelW * 0.08f, top + panelH * 0.34f, panelW * 0.11f, panelH * 0.15f, "<");
            drawArrowButton(c, margin + panelW * 0.81f, top + panelH * 0.34f, panelW * 0.11f, panelH * 0.15f, ">");
            drawArrowButton(c, margin + panelW + gap + panelW * 0.08f, top + panelH * 0.34f, panelW * 0.11f, panelH * 0.15f, "<");
            drawArrowButton(c, margin + panelW + gap + panelW * 0.81f, top + panelH * 0.34f, panelW * 0.11f, panelH * 0.15f, ">");

            float bx = w * 0.32f;
            float by = h * 0.865f;
            float bw = w * 0.36f;
            float bh = h * 0.095f;
            p.setColor(Color.rgb(28, 164, 241));
            c.drawRoundRect(bx, by, bx + bw, by + bh, bh * 0.18f, bh * 0.18f, p);
            p.setColor(Color.WHITE);
            p.setTextAlign(Paint.Align.CENTER);
            p.setTextSize(h * 0.032f);
            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.BOLD));
            c.drawText("START RACE", bx + bw * 0.5f, by + bh * 0.64f, p);
            p.setTextAlign(Paint.Align.LEFT);
        }

        void drawRace(Canvas c) {
            int w = getWidth();
            int h = getHeight();
            float trackPos = progress % TRACK_LENGTH;

            drawSky(c, w, h, selectedTrack, trackPos);
            drawRoad(c, w, h, selectedTrack, trackPos);
            drawAI(c, w, h, trackPos);
            drawPlayer(c, w, h);
            drawHud(c, w, h);
            drawControls(c, w, h);
        }

        void drawSky(Canvas c, int w, int h, int track, float trackPos) {
            int top;
            int bottom;
            if (track == 0) {
                top = Color.rgb(32, 72, 92);
                bottom = Color.rgb(154, 180, 173);
            } else if (track == 1) {
                top = Color.rgb(37, 61, 98);
                bottom = Color.rgb(238, 116, 50);
            } else {
                top = Color.rgb(4, 8, 27);
                bottom = Color.rgb(24, 31, 73);
            }

            LinearGradient sky = new LinearGradient(0, 0, 0, h * 0.52f, top, bottom, Shader.TileMode.CLAMP);
            p.setShader(sky);
            c.drawRect(0, 0, w, h * 0.55f, p);
            p.setShader(null);

            float horizon = h * 0.355f;
            if (track == 0) {
                p.setColor(Color.rgb(26, 58, 61));
                for (int i = 0; i < 9; i++) {
                    float x = i * w / 8f - w * 0.05f;
                    float peak = horizon - h * (0.05f + (i % 3) * 0.035f);
                    path.reset();
                    path.moveTo(x - w * 0.12f, horizon);
                    path.lineTo(x, peak);
                    path.lineTo(x + w * 0.14f, horizon);
                    path.close();
                    c.drawPath(path, p);
                }
            } else if (track == 1) {
                p.setColor(Color.rgb(117, 54, 29));
                for (int i = 0; i < 6; i++) {
                    float x = i * w / 5f;
                    float mh = h * (0.04f + (i % 3) * 0.025f);
                    c.drawRect(x - w * 0.08f, horizon - mh, x + w * 0.10f, horizon, p);
                }
                p.setColor(Color.rgb(255, 194, 85));
                c.drawCircle(w * 0.78f, h * 0.17f, h * 0.055f, p);
            } else {
                p.setColor(Color.rgb(16, 25, 52));
                for (int i = 0; i < 18; i++) {
                    float bw = w * (0.035f + (i % 4) * 0.007f);
                    float bh = h * (0.08f + (i * 7 % 12) * 0.012f);
                    float x = i * w / 17f;
                    c.drawRect(x - bw * .5f, horizon - bh, x + bw * .5f, horizon, p);
                    if (i % 2 == 0) {
                        p.setColor(i % 4 == 0 ? Color.rgb(0, 214, 255) : Color.rgb(255, 34, 170));
                        c.drawRect(x - bw * .35f, horizon - bh * .72f, x + bw * .35f, horizon - bh * .66f, p);
                        p.setColor(Color.rgb(16, 25, 52));
                    }
                }
            }
        }

        void drawRoad(Canvas c, int w, int h, int track, float trackPos) {
            final int segments = 54;
            final float far = 430f;
            float horizon = h * 0.355f;

            float prevY = horizon;
            float prevHalf = w * 0.045f;
            float prevCenter = w * 0.5f;

            for (int i = 1; i <= segments; i++) {
                float t = i / (float)segments;
                float depth = far * (1f - t);
                float y = horizon + (h - horizon) * (float)Math.pow(t, 1.68);
                float half = w * (0.045f + 0.49f * (float)Math.pow(t, 1.10));

                float d = trackPos + depth;
                float c1 = curveAt(track, d);
                float c2 = curveAt(track, trackPos + depth * 0.42f);
                float center = w * 0.5f + (c1 * w * 0.16f + c2 * w * 0.07f) * t - lateral * w * 0.20f * t;

                boolean bridge = track == 2 && isBridge(d);
                if (bridge) {
                    p.setColor(Color.rgb(6, 27, 48));
                    quad(c, 0, prevY, w, prevY, w, y, 0, y, p);
                } else if (track == 0) {
                    p.setColor((i & 1) == 0 ? Color.rgb(36, 72, 46) : Color.rgb(31, 66, 42));
                    quad(c, 0, prevY, w, prevY, w, y, 0, y, p);
                } else if (track == 1) {
                    p.setColor((i & 1) == 0 ? Color.rgb(126, 72, 39) : Color.rgb(116, 64, 34));
                    quad(c, 0, prevY, w, prevY, w, y, 0, y, p);
                } else {
                    p.setColor(Color.rgb(19, 22, 34));
                    quad(c, 0, prevY, w, prevY, w, y, 0, y, p);
                }

                float shoulderScale = track == 0 ? 1.10f : 1.08f;
                p.setColor(track == 2 ? Color.rgb(49, 54, 67) : Color.rgb(205, 203, 191));
                quad(c,
                    prevCenter - prevHalf * shoulderScale, prevY,
                    prevCenter + prevHalf * shoulderScale, prevY,
                    center + half * shoulderScale, y,
                    center - half * shoulderScale, y, p);

                if ((i / 3) % 2 == 0 && track != 2) {
                    p.setColor(track == 1 ? Color.rgb(170, 62, 30) : Color.rgb(205, 50, 45));
                    float curb = 1.045f;
                    quad(c,
                        prevCenter - prevHalf * curb, prevY,
                        prevCenter - prevHalf, prevY,
                        center - half, y,
                        center - half * curb, y, p);
                    quad(c,
                        prevCenter + prevHalf, prevY,
                        prevCenter + prevHalf * curb, prevY,
                        center + half * curb, y,
                        center + half, y, p);
                }

                p.setColor(track == 1 ? Color.rgb(53, 49, 45) : Color.rgb(42, 47, 54));
                quad(c,
                    prevCenter - prevHalf, prevY,
                    prevCenter + prevHalf, prevY,
                    center + half, y,
                    center - half, y, p);

                if (i % 5 <= 2) {
                    p.setColor(Color.rgb(224, 228, 220));
                    float laneW0 = prevHalf * 0.018f;
                    float laneW1 = half * 0.018f;
                    quad(c,
                        prevCenter - laneW0, prevY,
                        prevCenter + laneW0, prevY,
                        center + laneW1, y,
                        center - laneW1, y, p);
                }

                if (bridge && i % 4 == 0) {
                    p.setColor(Color.rgb(111, 141, 168));
                    float rail = half * 1.03f;
                    float thickness = Math.max(2f, h * 0.003f * t);
                    c.drawRect(center - rail - thickness, y - h * 0.025f * t, center - rail + thickness, y, p);
                    c.drawRect(center + rail - thickness, y - h * 0.025f * t, center + rail + thickness, y, p);
                }

                if (i % 4 == 0) drawTrackside(c, track, d, center, half, y, t, w, h);

                prevY = y;
                prevHalf = half;
                prevCenter = center;
            }

            if (track == 2) drawBridgeSignature(c, w, h, trackPos);
        }

        void drawTrackside(Canvas c, int track, float distance, float center, float half, float y, float t, int w, int h) {
            float size = h * 0.12f * t;
            if (size < 3) return;

            if (track == 0) {
                drawTree(c, center - half * 1.35f, y, size, false);
                if (((int)(distance / 30f) & 1) == 0) drawTree(c, center + half * 1.42f, y, size * .9f, true);
            } else if (track == 1) {
                p.setColor(Color.rgb(102, 46, 26));
                float rw = size * 0.85f;
                float rh = size * (1.0f + (((int)distance / 40) % 3) * .35f);
                c.drawRect(center - half * 1.52f - rw * .5f, y - rh, center - half * 1.52f + rw * .5f, y, p);
                if (((int)distance / 45) % 2 == 0)
                    c.drawRect(center + half * 1.45f - rw * .45f, y - rh * .72f, center + half * 1.45f + rw * .45f, y, p);
            } else if (!isBridge(distance)) {
                int block = ((int)(distance / 35f));
                int neon = (block & 1) == 0 ? Color.rgb(0, 208, 255) : Color.rgb(255, 35, 172);
                float bw = size * 0.72f;
                float bh = size * (1.4f + (block % 4) * .28f);
                p.setColor(Color.rgb(24, 31, 53));
                c.drawRect(center - half * 1.48f - bw, y - bh, center - half * 1.48f, y, p);
                c.drawRect(center + half * 1.48f, y - bh * .82f, center + half * 1.48f + bw, y, p);
                p.setColor(neon);
                c.drawRect(center - half * 1.48f - bw * .9f, y - bh * .68f, center - half * 1.48f - bw * .08f, y - bh * .60f, p);
            }
        }

        void drawTree(Canvas c, float x, float y, float size, boolean alternate) {
            p.setColor(Color.rgb(87, 55, 31));
            c.drawRect(x - size * .07f, y - size * .42f, x + size * .07f, y, p);
            p.setColor(alternate ? Color.rgb(35, 104, 62) : Color.rgb(25, 88, 49));
            path.reset();
            path.moveTo(x, y - size * 1.12f);
            path.lineTo(x - size * .42f, y - size * .30f);
            path.lineTo(x + size * .42f, y - size * .30f);
            path.close();
            c.drawPath(path, p);
            path.reset();
            path.moveTo(x, y - size * .88f);
            path.lineTo(x - size * .48f, y - size * .10f);
            path.lineTo(x + size * .48f, y - size * .10f);
            path.close();
            c.drawPath(path, p);
        }

        void drawBridgeSignature(Canvas c, int w, int h, float trackPos) {
            float phase = (trackPos % TRACK_LENGTH) / TRACK_LENGTH;
            if (phase > .08f && phase < .43f) {
                float horizon = h * .355f;
                float towerScale = 1f - Math.abs(phase - .25f) / .17f;
                towerScale = clamp(towerScale, 0, 1);
                if (towerScale > .08f) {
                    float towerY = horizon + h * .18f;
                    float towerH = h * (.15f + .25f * towerScale);
                    float left = w * .27f;
                    float right = w * .73f;

                    p.setColor(Color.rgb(115, 149, 177));
                    c.drawRect(left - w * .012f, towerY - towerH, left + w * .012f, towerY, p);
                    c.drawRect(right - w * .012f, towerY - towerH, right + w * .012f, towerY, p);

                    p.setStyle(Paint.Style.STROKE);
                    p.setStrokeWidth(Math.max(2f, h * .004f));
                    p.setColor(Color.rgb(94, 177, 216));
                    path.reset();
                    path.moveTo(left, towerY - towerH * .9f);
                    path.quadTo(w * .5f, towerY - towerH * .25f, right, towerY - towerH * .9f);
                    c.drawPath(path, p);
                    p.setStyle(Paint.Style.FILL);
                }
            }
        }

        void drawAI(Canvas c, int w, int h, float trackPos) {
            for (int i = 0; i < AI_COUNT; i++) {
                float rel = (aiProgress[i] % TRACK_LENGTH) - trackPos;
                if (rel < 0) rel += TRACK_LENGTH;
                if (rel < 8f || rel > 410f) continue;

                float t = 1f - rel / 410f;
                float y = h * .355f + (h - h * .355f) * (float)Math.pow(t, 1.68);
                float roadHalf = w * (.045f + .49f * (float)Math.pow(t, 1.10));
                float center = w * .5f +
                    (curveAt(selectedTrack, trackPos + rel) * w * .16f +
                     curveAt(selectedTrack, trackPos + rel * .42f) * w * .07f) * t -
                    lateral * w * .20f * t;

                float lane = aiLane[i] + (float)Math.sin(aiProgress[i] * .006f + i) * .08f;
                float x = center + lane * roadHalf * .65f;
                float scale = .18f + t * 1.08f;
                drawOpponentCar(c, x, y, h * .052f * scale, carColors[(i + selectedCar + 1) % 3]);
            }
        }

        void drawOpponentCar(Canvas c, float x, float y, float size, int color) {
            float bw = size * 1.6f;
            float bh = size;
            p.setColor(Color.argb(80, 0, 0, 0));
            c.drawOval(x - bw * .62f, y - bh * .05f, x + bw * .62f, y + bh * .20f, p);

            p.setColor(color);
            path.reset();
            path.moveTo(x - bw * .55f, y);
            path.lineTo(x - bw * .42f, y - bh * .72f);
            path.lineTo(x - bw * .20f, y - bh);
            path.lineTo(x + bw * .20f, y - bh);
            path.lineTo(x + bw * .42f, y - bh * .72f);
            path.lineTo(x + bw * .55f, y);
            path.close();
            c.drawPath(path, p);

            p.setColor(Color.rgb(20, 30, 42));
            c.drawRect(x - bw * .22f, y - bh * .82f, x + bw * .22f, y - bh * .58f, p);
            p.setColor(Color.rgb(255, 55, 45));
            c.drawRect(x - bw * .38f, y - bh * .22f, x - bw * .16f, y - bh * .12f, p);
            c.drawRect(x + bw * .16f, y - bh * .22f, x + bw * .38f, y - bh * .12f, p);
        }

        void drawPlayer(Canvas c, int w, int h) {
            float x = w * .5f + steer * w * .012f;
            float y = h * .92f;
            float size = h * .17f;
            int color = carColors[selectedCar];

            p.setColor(Color.argb(95, 0, 0, 0));
            c.drawOval(x - size * .72f, y - size * .04f, x + size * .72f, y + size * .22f, p);

            p.setColor(Color.rgb(20, 22, 25));
            c.drawRoundRect(x - size * .68f, y - size * .40f, x - size * .47f, y + size * .03f, size * .05f, size * .05f, p);
            c.drawRoundRect(x + size * .47f, y - size * .40f, x + size * .68f, y + size * .03f, size * .05f, size * .05f, p);

            p.setColor(color);
            path.reset();
            path.moveTo(x - size * .58f, y);
            path.lineTo(x - size * .50f, y - size * .60f);
            path.lineTo(x - size * .30f, y - size * .82f);
            path.lineTo(x + size * .30f, y - size * .82f);
            path.lineTo(x + size * .50f, y - size * .60f);
            path.lineTo(x + size * .58f, y);
            path.close();
            c.drawPath(path, p);

            p.setColor(Color.rgb(24, 42, 58));
            path.reset();
            path.moveTo(x - size * .28f, y - size * .65f);
            path.lineTo(x - size * .20f, y - size * .79f);
            path.lineTo(x + size * .20f, y - size * .79f);
            path.lineTo(x + size * .28f, y - size * .65f);
            path.close();
            c.drawPath(path, p);

            p.setColor(Color.rgb(255, 54, 44));
            c.drawRect(x - size * .42f, y - size * .25f, x - size * .19f, y - size * .17f, p);
            c.drawRect(x + size * .19f, y - size * .25f, x + size * .42f, y - size * .17f, p);

            p.setColor(Color.rgb(235, 238, 240));
            c.drawRect(x - size * .22f, y - size * .05f, x + size * .22f, y + size * .02f, p);
        }

        void drawHud(Canvas c, int w, int h) {
            int lap = Math.min(LAPS, (int)(progress / TRACK_LENGTH) + 1);
            int pos = currentPosition();

            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.BOLD));
            p.setColor(Color.WHITE);
            p.setTextAlign(Paint.Align.LEFT);
            p.setTextSize(h * .037f);
            c.drawText("P" + pos + " / " + (AI_COUNT + 1), w * .035f, h * .075f, p);

            p.setTextSize(h * .027f);
            p.setColor(Color.rgb(170, 202, 220));
            c.drawText("LAP " + lap + " / " + LAPS, w * .035f, h * .119f, p);

            p.setTextAlign(Paint.Align.RIGHT);
            p.setColor(Color.WHITE);
            p.setTextSize(h * .062f);
            c.drawText(String.format(Locale.US, "%03d", Math.round(speed)), w * .94f, h * .085f, p);
            p.setTextSize(h * .021f);
            p.setColor(Color.rgb(141, 199, 226));
            c.drawText("KM/H", w * .94f, h * .119f, p);

            p.setTextAlign(Paint.Align.CENTER);
            p.setTextSize(h * .020f);
            p.setColor(Color.argb(190, 205, 222, 234));
            c.drawText(trackNames[selectedTrack], w * .5f, h * .06f, p);
            p.setTextAlign(Paint.Align.LEFT);
        }

        void drawControls(Canvas c, int w, int h) {
            joyCx = w * .13f;
            joyCy = h * .78f;
            joyR = h * .135f;

            gasCx = w * .885f;
            gasCy = h * .77f;
            gasR = h * .115f;

            brakeCx = w * .74f;
            brakeCy = h * .82f;
            brakeR = h * .092f;

            p.setColor(Color.argb(42, 255, 255, 255));
            c.drawCircle(joyCx, joyCy, joyR, p);
            p.setColor(Color.argb(115, 255, 255, 255));
            c.drawCircle(joyCx + steer * joyR * .62f, joyCy, joyR * .36f, p);

            p.setColor(gas ? Color.argb(190, 32, 222, 106) : Color.argb(88, 32, 222, 106));
            c.drawCircle(gasCx, gasCy, gasR, p);
            p.setColor(brake ? Color.argb(200, 244, 61, 45) : Color.argb(92, 244, 61, 45));
            c.drawCircle(brakeCx, brakeCy, brakeR, p);

            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.BOLD));
            p.setTextAlign(Paint.Align.CENTER);
            p.setTextSize(h * .027f);
            p.setColor(Color.WHITE);
            c.drawText("GAS", gasCx, gasCy + h * .010f, p);
            p.setTextSize(h * .022f);
            c.drawText("BRAKE", brakeCx, brakeCy + h * .008f, p);
            p.setTextAlign(Paint.Align.LEFT);
        }

        void drawResults(Canvas c) {
            int w = getWidth();
            int h = getHeight();

            p.setColor(Color.argb(220, 5, 12, 22));
            c.drawRoundRect(w * .28f, h * .19f, w * .72f, h * .81f, h * .025f, h * .025f, p);

            p.setTextAlign(Paint.Align.CENTER);
            p.setTypeface(android.graphics.Typeface.create("sans-serif", android.graphics.Typeface.BOLD));
            p.setColor(Color.rgb(101, 210, 255));
            p.setTextSize(h * .031f);
            c.drawText("RACE COMPLETE", w * .5f, h * .31f, p);

            p.setColor(Color.WHITE);
            p.setTextSize(h * .105f);
            c.drawText("P" + finalPosition, w * .5f, h * .50f, p);

            p.setColor(Color.rgb(165, 192, 208));
            p.setTextSize(h * .022f);
            c.drawText(trackNames[selectedTrack] + "  •  " + carNames[selectedCar], w * .5f, h * .59f, p);

            p.setColor(Color.rgb(26, 162, 239));
            c.drawRoundRect(w * .36f, h * .66f, w * .64f, h * .745f, h * .012f, h * .012f, p);
            p.setColor(Color.WHITE);
            p.setTextSize(h * .027f);
            c.drawText("RETURN TO GARAGE", w * .5f, h * .715f, p);
            p.setTextAlign(Paint.Align.LEFT);
        }

        void drawTrackPreview(Canvas c, int track, float x, float y, float w, float h) {
            p.setColor(track == 0 ? Color.rgb(24, 63, 51) : track == 1 ? Color.rgb(117, 57, 31) : Color.rgb(12, 20, 50));
            c.drawRoundRect(x, y, x + w, y + h, h * .05f, h * .05f, p);

            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(Math.max(3f, h * .035f));
            p.setColor(track == 2 ? Color.rgb(48, 200, 255) : Color.rgb(210, 215, 207));
            path.reset();
            path.moveTo(x + w * .13f, y + h * .63f);
            path.cubicTo(x + w * .25f, y + h * .16f, x + w * .55f, y + h * .85f, x + w * .82f, y + h * .38f);
            path.cubicTo(x + w * .66f, y + h * .16f, x + w * .38f, y + h * .28f, x + w * .13f, y + h * .63f);
            c.drawPath(path, p);
            p.setStyle(Paint.Style.FILL);

            if (track == 2) {
                p.setColor(Color.rgb(255, 36, 177));
                c.drawRect(x + w * .60f, y + h * .20f, x + w * .84f, y + h * .25f, p);
            }
        }

        void drawCarHero(Canvas c, float x, float y, float width, int color) {
            float h = width * .30f;
            p.setColor(Color.argb(80, 0, 0, 0));
            c.drawOval(x - width * .52f, y + h * .30f, x + width * .52f, y + h * .57f, p);

            p.setColor(Color.rgb(20, 22, 25));
            c.drawOval(x - width * .45f, y + h * .10f, x - width * .30f, y + h * .62f, p);
            c.drawOval(x + width * .30f, y + h * .10f, x + width * .45f, y + h * .62f, p);

            p.setColor(color);
            path.reset();
            path.moveTo(x - width * .47f, y + h * .38f);
            path.lineTo(x - width * .35f, y - h * .03f);
            path.lineTo(x - width * .16f, y - h * .30f);
            path.lineTo(x + width * .17f, y - h * .30f);
            path.lineTo(x + width * .38f, y - h * .02f);
            path.lineTo(x + width * .48f, y + h * .38f);
            path.close();
            c.drawPath(path, p);

            p.setColor(Color.rgb(24, 43, 59));
            c.drawRoundRect(x - width * .16f, y - h * .22f, x + width * .17f, y + h * .02f, h * .06f, h * .06f, p);
        }

        void drawPanel(Canvas c, float x, float y, float w, float h) {
            p.setColor(Color.argb(235, 14, 23, 35));
            c.drawRoundRect(x, y, x + w, y + h, h * .025f, h * .025f, p);
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(2f);
            p.setColor(Color.rgb(39, 64, 81));
            c.drawRoundRect(x, y, x + w, y + h, h * .025f, h * .025f, p);
            p.setStyle(Paint.Style.FILL);
        }

        void drawBar(Canvas c, float x, float y, float w, float h, float amount) {
            p.setColor(Color.rgb(38, 52, 65));
            c.drawRoundRect(x, y, x + w, y + h, h * .5f, h * .5f, p);
            p.setColor(Color.rgb(45, 180, 241));
            c.drawRoundRect(x, y, x + w * amount, y + h, h * .5f, h * .5f, p);
        }

        void drawArrowButton(Canvas c, float x, float y, float w, float h, String label) {
            p.setColor(Color.rgb(31, 45, 59));
            c.drawRoundRect(x, y, x + w, y + h, h * .18f, h * .18f, p);
            p.setColor(Color.WHITE);
            p.setTextAlign(Paint.Align.CENTER);
            p.setTextSize(h * .48f);
            c.drawText(label, x + w * .5f, y + h * .66f, p);
            p.setTextAlign(Paint.Align.LEFT);
        }

        @Override
        public boolean onTouchEvent(MotionEvent e) {
            int action = e.getActionMasked();
            int index = e.getActionIndex();
            int pointerId = e.getPointerId(index);
            float x = e.getX(index);
            float y = e.getY(index);

            if (state == MENU) {
                if (action == MotionEvent.ACTION_UP) handleMenuTap(x, y);
                return true;
            }

            if (state == RESULTS) {
                if (action == MotionEvent.ACTION_UP && x > getWidth() * .33f && x < getWidth() * .67f &&
                    y > getHeight() * .62f && y < getHeight() * .78f) {
                    state = MENU;
                    speed = 0;
                }
                return true;
            }

            if (action == MotionEvent.ACTION_DOWN || action == MotionEvent.ACTION_POINTER_DOWN) {
                if (distance(x, y, gasCx, gasCy) < gasR * 1.35f && gasPointer < 0) {
                    gasPointer = pointerId;
                    gas = true;
                } else if (distance(x, y, brakeCx, brakeCy) < brakeR * 1.45f && brakePointer < 0) {
                    brakePointer = pointerId;
                    brake = true;
                } else if (x < getWidth() * .38f && steerPointer < 0) {
                    steerPointer = pointerId;
                    updateSteer(x);
                }
            } else if (action == MotionEvent.ACTION_MOVE) {
                for (int i = 0; i < e.getPointerCount(); i++) {
                    if (e.getPointerId(i) == steerPointer) updateSteer(e.getX(i));
                }
            } else if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP || action == MotionEvent.ACTION_CANCEL) {
                if (pointerId == gasPointer) { gasPointer = -1; gas = false; }
                if (pointerId == brakePointer) { brakePointer = -1; brake = false; }
                if (pointerId == steerPointer) { steerPointer = -1; steer = 0; }

                if (action == MotionEvent.ACTION_CANCEL) {
                    gasPointer = brakePointer = steerPointer = -1;
                    gas = brake = false;
                    steer = 0;
                }
            }

            return true;
        }

        void handleMenuTap(float x, float y) {
            int w = getWidth();
            int h = getHeight();
            float margin = w * .045f;
            float gap = w * .025f;
            float top = h * .23f;
            float panelH = h * .59f;
            float panelW = (w - margin * 2 - gap) * .5f;

            if (y > top + panelH * .30f && y < top + panelH * .55f) {
                if (x > margin && x < margin + panelW * .25f) {
                    selectedCar = (selectedCar + carNames.length - 1) % carNames.length;
                    return;
                }
                if (x > margin + panelW * .75f && x < margin + panelW) {
                    selectedCar = (selectedCar + 1) % carNames.length;
                    return;
                }

                float tx = margin + panelW + gap;
                if (x > tx && x < tx + panelW * .25f) {
                    selectedTrack = (selectedTrack + trackNames.length - 1) % trackNames.length;
                    return;
                }
                if (x > tx + panelW * .75f && x < tx + panelW) {
                    selectedTrack = (selectedTrack + 1) % trackNames.length;
                    return;
                }
            }

            if (x > w * .29f && x < w * .71f && y > h * .83f && y < h * .98f)
                startRace();
        }

        void updateSteer(float x) {
            steer = clamp((x - joyCx) / Math.max(1f, joyR * .72f), -1, 1);
        }

        boolean isBridge(float distance) {
            float phase = positiveMod(distance, TRACK_LENGTH) / TRACK_LENGTH;
            return phase > .12f && phase < .36f;
        }

        float curveAt(int track, float distance) {
            if (track == 0) {
                return (float)(Math.sin(distance / 118f) * .74 + Math.sin(distance / 47f) * .31);
            } else if (track == 1) {
                return (float)(Math.sin(distance / 255f) * .43 + Math.sin(distance / 102f) * .17);
            } else {
                float base = (float)(Math.sin(distance / 172f) * .55 + Math.sin(distance / 66f) * .22);
                if (isBridge(distance)) base *= .26f;
                return base;
            }
        }

        static void quad(Canvas c, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, Paint paint) {
            Path q = new Path();
            q.moveTo(x1, y1);
            q.lineTo(x2, y2);
            q.lineTo(x3, y3);
            q.lineTo(x4, y4);
            q.close();
            c.drawPath(q, paint);
        }

        static float distance(float x1, float y1, float x2, float y2) {
            float dx = x1 - x2;
            float dy = y1 - y2;
            return (float)Math.sqrt(dx * dx + dy * dy);
        }

        static float positiveMod(float a, float b) {
            float r = a % b;
            return r < 0 ? r + b : r;
        }

        static float clamp(float v, float min, float max) {
            return Math.max(min, Math.min(max, v));
        }
    }
}
