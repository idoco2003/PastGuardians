const { GoogleGenerativeAI } = require("@google/generative-ai");
const fs = require("fs");
const path = require("path");

const GEMINI_API_KEY = "AIzaSyBErsm92QETy9-h6FFF_CnD9X3WCwOboVI";

async function generateImage(prompt, outputPath) {
  const genAI = new GoogleGenerativeAI(GEMINI_API_KEY);

  // Use Gemini 2.0 Flash for image generation
  const model = genAI.getGenerativeModel({
    model: "gemini-2.0-flash-exp-image-generation",
    generationConfig: {
      responseModalities: ["image", "text"],
    }
  });

  console.log(`Generating: ${outputPath}`);
  console.log(`Prompt: ${prompt.substring(0, 100)}...`);

  try {
    const response = await model.generateContent(prompt);
    const result = response.response;

    // Extract image from response
    for (const part of result.candidates[0].content.parts) {
      if (part.inlineData) {
        const imageData = part.inlineData.data;
        const buffer = Buffer.from(imageData, "base64");

        // Ensure directory exists
        const dir = path.dirname(outputPath);
        if (!fs.existsSync(dir)) {
          fs.mkdirSync(dir, { recursive: true });
        }

        fs.writeFileSync(outputPath, buffer);
        console.log(`Saved to: ${outputPath}`);
        return true;
      }
    }

    console.log("No image in response");
    return false;
  } catch (error) {
    console.error(`Error: ${error.message}`);
    return false;
  }
}

const BASE_PATH = "C:/Users/Ido/TempusPrime";

// Intruder prompts
const intruders = {
  "ornithopter": {
    prompt: "Leonardo da Vinci flying machine, wooden ornithopter with canvas wings, brass and bronze mechanical parts, Renaissance invention, flapping wing mechanism visible, steampunk aesthetic, warm bronze glow, historical flying contraption, intricate wooden frame, game asset, digital art, child-friendly, transparent background",
    path: `${BASE_PATH}/Assets/Sprites/Intruders/HistoricalMachines/ornithopter.png`
  },
  "pteranodon": {
    prompt: "A friendly pteranodon flying creature, prehistoric flying reptile, amber-orange color scheme, warm golden glow around it, large wingspan, curious expression, non-threatening pose, slightly cartoonish proportions, viewed from below angle, digital art, game asset, child-friendly, mobile game style, transparent background",
    path: `${BASE_PATH}/Assets/Sprites/Intruders/Prehistoric/pteranodon.png`
  },
  "griffin": {
    prompt: "Noble griffin mythical creature, lion body with eagle head and wings, royal purple and gold color scheme, proud stance, majestic feathers, friendly fierce expression, heraldic pose, magical purple glow, fantasy game art style, mobile game character, child-friendly design, transparent background",
    path: `${BASE_PATH}/Assets/Sprites/Intruders/Mythological/griffin.png`
  },
  "phoenix": {
    prompt: "Magnificent phoenix bird, fire bird rising, purple and magenta flames with gold accents, elegant long tail feathers, rebirth symbolism, warm magical glow, graceful flight pose, fantasy creature design, awe-inspiring, mobile game asset, child-friendly, transparent background",
    path: `${BASE_PATH}/Assets/Sprites/Intruders/Mythological/phoenix.png`
  },
  "time_echo": {
    prompt: "Ghostly time echo, translucent human silhouette, rainbow prismatic edges, glitching visual effect, afterimage trails, temporal distortion, person frozen mid-motion, chromatic aberration effect, mysterious but not scary, sci-fi time travel aesthetic, game character, transparent background",
    path: `${BASE_PATH}/Assets/Sprites/Intruders/TimeAnomalies/time_echo.png`
  },
  "atlantean_craft": {
    prompt: "Atlantean flying vehicle, ancient advanced technology, teal and gold color scheme, crystal-powered propulsion, glowing cyan energy, sleek organic curves, underwater civilization aesthetic, mysterious glyphs, silent hovering pose, lost technology design, game asset, transparent background",
    path: `${BASE_PATH}/Assets/Sprites/Intruders/LostCivilizations/atlantean_craft.png`
  }
};

// Main execution
async function main() {
  const target = process.argv[2] || "ornithopter";

  if (target === "all") {
    for (const [name, data] of Object.entries(intruders)) {
      await generateImage(data.prompt, data.path);
      await new Promise(r => setTimeout(r, 2000)); // Rate limit
    }
  } else if (intruders[target]) {
    await generateImage(intruders[target].prompt, intruders[target].path);
  } else {
    console.log("Usage: node generate_image.js [ornithopter|pteranodon|griffin|phoenix|time_echo|all]");
    console.log("Available:", Object.keys(intruders).join(", "));
  }
}

main();
