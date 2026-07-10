This is a shitty decompiler for Fmod Banks, but feature complete specifically for the bank I was targeting.

this inspired this, but this also sucks: https://github.com/doggywatty/FMOD-Decompiler

I gave up midway through this realizing I didn't really want to write a generalized decompiler, I just needed to recover specifically the 2018 Tabg project.
You could absolutely use this as a starting off point, but It can't be applied to other banks easily, and it's specifically for 1.10 fmod studio.
Big chunk of it is slop because I realized that I already had made the test project, and that means I could just run an LLM against it to get the right outputs, and I don't care about this project.

If you're looking to decompile your own Fmod Bank, given you only have to do one ever, it's probably just a good idea to dump all of the features that it uses, and make something bespoke 

Don't ask me about any feature past extracting the parameters, it's total slop, didnt read it, just validated the output 

also check out DebugHelper its peak, it's also slop. I should make some actual good software like it tho, a full debugging lib like it would be funny.