using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;

public partial class DamageHandler : Node
{
	private const float PIXEL_HEALTH_CHECKER = 10000;
	private Vector2 EMPTY_VECTOR = new Vector2(-1, -1);

	[Export(PropertyHint.File, "*.png,")]private string spritePath;
	[Export(PropertyHint.File, "*.png,")]private string spriteHMPath;
	private Image sprite;
	private Image spriteHM;
	public float pixelHP = 10;
	public struct PixelIntegity {

		public PixelIntegity(Color color, float bluntRes = .2f, float maxHealth = 100){
			pixelHMColor = color;
			bluntResistance = bluntRes;
			hp = maxHealth;
			hpMax = maxHealth;
		}
		public float hp;
		public float hpMax;
		public float bluntResistance;
		public Color pixelHMColor;
	}
	private PixelIntegity[,] pixleArray;
	private int spriteWidth;
	private int spriteHeight;
	public override void _Ready()
	{
		sprite = Image.LoadFromFile(spritePath);
		spriteHM = Image.LoadFromFile(spriteHMPath);
		spriteWidth = sprite.GetWidth();
		spriteHeight = sprite.GetHeight();

		//Container to hold information about health and damage resistances
		pixleArray = new PixelIntegity[spriteWidth,spriteHeight];
		//Debug.Print("Rows: " + pixleArray.GetLength(0) + " Colomns: " + pixleArray.GetLength(1));
		//Debug.Print(spriteHM.GetPixel(20, 39).ToString());

		//Populate pixleArray from the sprite
		for(int i = 0; i < spriteWidth; i++) {
			for(int j = 0; j < spriteHeight; j++) {
				//Debug.Print(i + ", " +  j);
				//Debug.Print("(" + i + ", " +  j + "): " + spriteHM.GetPixel(i, j).ToString());
				pixleArray[i, j] = new PixelIntegity
				{
					pixelHMColor = spriteHM.GetPixel(i, j),
					bluntResistance = spriteHM.GetPixel(i, j).R,
					hpMax = pixelHP,
					hp = pixelHP
				};
			}
			//Debug.Print("Add new HM color row at row: " + i);

		}
	}
	public void Damage(Vector2 point, int size = 0, float damage = 0) {
		if (size <= 0 || damage <= 0){
			Debug.Print("Size or Damage is 0");
			return;
		}
		int pointx = (int)MathF.Floor(point.X) + spriteWidth/2;
		int pointy = (int)MathF.Floor(point.Y) + spriteHeight/2;
		Vector2 targetPixelCords = new Vector2(pointx, pointy);
		List<Vector2> expandingPixelCords = new List<Vector2>
		{
			targetPixelCords
		};
		List<Vector2> outerPixelCords = new List<Vector2>(){
			targetPixelCords
		};

		//Gets a list of all the effected pixels based on the size of the attack
		for(int i = 0; i < size; i++){
			List<Vector2> outerPixelCordsBuffer = new List<Vector2>();
			outerPixelCords.ForEach(x => outerPixelCordsBuffer.Add(x));
			//Debug.Print("outerPixelCordsBuffer: ");
			//outerPixelCordsBuffer.ForEach(x => Debug.Print(x.ToString()));

			foreach(Vector2 v in outerPixelCordsBuffer){
				outerPixelCords.Remove(v);
				outerPixelCords.AddRange(FindAdjacent(v, expandingPixelCords));
				//outerPixelCords.ForEach(x => Debug.Print(x.ToString()));
			}
			outerPixelCords = outerPixelCords.Distinct().ToList();
			expandingPixelCords.AddRange(outerPixelCords);
			//Debug.Print("Expaning Pixel Cords");
			//expandingPixelCords.ForEach(x => Debug.Print(x.ToString()));
			outerPixelCordsBuffer.Clear();
		}

		//Calculate Damage Values
		var randomDamageGenerator = new RandomNumberGenerator();
		List<float> damageDistribution = new List<float>();
		for (int i = 0; i < expandingPixelCords.Count; i++)
			damageDistribution.Add(randomDamageGenerator.RandfRange(0f, 1.0f));
		float distributionTotal = damageDistribution.Sum();
		for (int i = 0; i < damageDistribution.Count; i++){
			damageDistribution[i] /= distributionTotal;
			damageDistribution[i] *= damage;
		}
		damageDistribution = damageDistribution.OrderByDescending(i => i).ToList();
		//damageDistribution.ForEach(x => Debug.Print(x.ToString()));


		//Apply Damage Values to each pixel
		for (int i = 0; i < expandingPixelCords.Count; i++){
			DamageAplication(damageDistribution[i], expandingPixelCords[i]);
		}

		RenderDamage();

		//Color Display For Test
		/*
		Image targetPixelsSprite = sprite;
		var rng = new RandomNumberGenerator();
		float randR = rng.RandfRange(0, 1.0f);
		float randG = rng.RandfRange(0, 1.0f);
		float randB = rng.RandfRange(0, 1.0f);
		Color color = new Color(randR, randG, randB, 1);
		foreach(Vector2 v in expandingPixelCords){
			targetPixelsSprite.SetPixel((int)v.X, (int)v.Y, color);
		}
		Sprite2D sprite2D = (Sprite2D)GetParent().GetChild(0);
		sprite2D.Texture = ImageTexture.CreateFromImage(targetPixelsSprite);
		*/
		
	}
	private List<Vector2> FindAdjacent(Vector2 cordsPrime, List<Vector2> listOfCords){
		//Debug.Print("Starting FindAdjacent loop");
		//Debug.Print(listOfCords.Contains(new Vector2(cordsPrime.X, cordsPrime.Y+1)).ToString());
		List<Vector2> cords = new List<Vector2>();
		if (cordsPrime.Y+1 < spriteHeight && !listOfCords.Contains(new Vector2(cordsPrime.X, cordsPrime.Y+1)))
			cords.Add(new Vector2(cordsPrime.X, cordsPrime.Y+1));
		if (cordsPrime.Y-1 >= 0 && !listOfCords.Contains(new Vector2(cordsPrime.X, cordsPrime.Y-1)))
			cords.Add(new Vector2(cordsPrime.X, cordsPrime.Y-1));
		if (cordsPrime.X+1 < spriteWidth && !listOfCords.Contains(new Vector2(cordsPrime.X+1, cordsPrime.Y)))
			cords.Add(new Vector2(cordsPrime.X+1, cordsPrime.Y));
		if (cordsPrime.X-1 >= 0 && !listOfCords.Contains(new Vector2(cordsPrime.X-1, cordsPrime.Y)))
			cords.Add(new Vector2(cordsPrime.X-1, cordsPrime.Y));
		//Debug.Print("fininished list: ");
		//foreach(Vector2 v in cords)
		//	Debug.Print(v.ToString());
		return cords;
	}
	private List<Vector2> FindAdjacent(Vector2 cordsPrime) {
		List<Vector2> cords = new List<Vector2>();
		if (cordsPrime.Y+1 < spriteHeight)
			cords.Add(new Vector2(cordsPrime.X, cordsPrime.Y+1));
		if (cordsPrime.Y-1 >= 0)
			cords.Add(new Vector2(cordsPrime.X, cordsPrime.Y-1));
		if (cordsPrime.X+1 < spriteWidth)
			cords.Add(new Vector2(cordsPrime.X+1, cordsPrime.Y));
		if (cordsPrime.X-1 >= 0)
			cords.Add(new Vector2(cordsPrime.X-1, cordsPrime.Y));
		return cords;
	}
	private Vector2[] CheckAdjacent(Vector2 cordsPrime){
		//North: 0 East: 1 South: 2 West: 3
		Vector2[] checkedCords = {EMPTY_VECTOR,EMPTY_VECTOR,EMPTY_VECTOR,EMPTY_VECTOR};
		if (cordsPrime.Y+1 < spriteHeight)
			checkedCords[2] = new Vector2(cordsPrime.X, cordsPrime.Y+1);
		if (cordsPrime.Y-1 >= 0)
			checkedCords[0] = new Vector2(cordsPrime.X, cordsPrime.Y-1);
		if (cordsPrime.X+1 < spriteWidth)
			checkedCords[1] = new Vector2(cordsPrime.X+1, cordsPrime.Y);
		if (cordsPrime.X-1 >= 0)
			checkedCords[3] = new Vector2(cordsPrime.X-1, cordsPrime.Y);
		return checkedCords;
	}
	private void DamageAplication(float damage, Vector2 pixelCords){
		float damageToApply = damage;
		if (pixleArray[(int)pixelCords.X, (int)pixelCords.Y].hp >= damageToApply){
			pixleArray[(int)pixelCords.X, (int)pixelCords.Y].hp -= damageToApply;
			//Debug.Print("Dealt " + damageToApply + "Damage to pixel at " + pixelCords.ToString());
		}
		else if (pixleArray[(int)pixelCords.X, (int)pixelCords.Y].hp < damageToApply){
			//Debug.Print("Dealt " + damageToApply + "Damage to pixel at " + pixelCords.ToString() + ". Dealing Excess Damage!");
			damageToApply -= pixleArray[(int)pixelCords.X, (int)pixelCords.Y].hp;
			pixleArray[(int)pixelCords.X, (int)pixelCords.Y].hp = 0;
			ExcessDamageAplication(damageToApply, pixelCords);
		}
	}
	private void ExcessDamageAplication(float damage, Vector2 pixelCords){
		//Ceck for adjacent pixels with HP remaining and deal damage to the lowest health pixel
		List<Vector2> adjacentPixels = FindAdjacent(pixelCords);
		Vector2 lowestHPPixelCords = new Vector2(0,0);
		float lowestHPPixelHealth = PIXEL_HEALTH_CHECKER;
		foreach (Vector2 v in adjacentPixels){
			if (pixleArray[(int)v.X, (int)v.Y].hp > 0 && pixleArray[(int)v.X, (int)v.Y].hp < lowestHPPixelHealth){
				lowestHPPixelHealth = pixleArray[(int)v.X, (int)v.Y].hp;
				lowestHPPixelCords = new Vector2((int)v.X, (int)v.Y);
			}
		}
		if (lowestHPPixelHealth != PIXEL_HEALTH_CHECKER){
			DamageAplication(damage, lowestHPPixelCords);
			return;
		}
		//Checks to see which quadrant of the sprite pixel is in and moves randomly in and outward direction
		Vector2[] availableDirections = CheckAdjacent(pixelCords);
		var randomDirectionGenerator = new RandomNumberGenerator();
		int randomDirection = randomDirectionGenerator.RandiRange(0, 1);
		//Top Left
		if (pixelCords.X <= spriteWidth/2 && pixelCords.Y <= spriteHeight/2){
			if (randomDirection == 0 && availableDirections[0] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X, pixelCords.Y-1));
			else if (randomDirection == 1 && availableDirections[3] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X-1, pixelCords.Y));
			return;

		}
		//Top Right
		else if (pixelCords.X >= spriteWidth/2 && pixelCords.Y < spriteHeight/2){
			if (randomDirection == 0 && availableDirections[0] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X, pixelCords.Y-1));
			else if (randomDirection == 1 && availableDirections[1] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X+1, pixelCords.Y));
			return;
		}
		//Bottom Left
		else if (pixelCords.X < spriteWidth/2 && pixelCords.Y > spriteHeight/2){
			if (randomDirection == 0 && availableDirections[2] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X, pixelCords.Y+1));
			else if (randomDirection == 1 && availableDirections[3] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X-1, pixelCords.Y));
			return;
		}
		//Bottom Right
		else if (pixelCords.X > spriteWidth/2 && pixelCords.Y >= spriteHeight/2){
			if (randomDirection == 0 && availableDirections[2] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X, pixelCords.Y+1));
			else if (randomDirection == 1 && availableDirections[1] != EMPTY_VECTOR)
				ExcessDamageAplication(damage, new Vector2(pixelCords.X+1, pixelCords.Y));
			return;
		}
	}
	private void RenderDamage(){
		Image targetPixelsSprite = new Image();
		targetPixelsSprite.CopyFrom(sprite);
		Color newColor;
		Color spriteColor;
		for(int i = 0; i < spriteWidth; i++) {
			for(int j = 0; j < spriteHeight; j++) {
				spriteColor = sprite.GetPixel(i, j);
				float damageBlackness = 1 - (pixleArray[i, j].hp / pixleArray[i, j].hpMax);
				newColor = new Color(spriteColor.R - damageBlackness, spriteColor.G - damageBlackness, spriteColor.B - damageBlackness, 1);
				targetPixelsSprite.SetPixel(i, j, newColor);
			}
		}
		Sprite2D sprite2D = (Sprite2D)GetParent().GetChild(0);
		sprite2D.Texture = ImageTexture.CreateFromImage(targetPixelsSprite);
	}
}
