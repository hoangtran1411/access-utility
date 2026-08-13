# 00 - Beginner's Guide to AccessUtility

Welcome to **AccessUtility**! If you are new to the team or just getting started with programming, this guide is written specifically for you. We will break down exactly what this project does without the confusing jargon.

## 🕰️ The History Lesson
In the late 1990s and early 2000s, businesses loved **Microsoft Access 97** (`.mdb` files). It was an easy way to store data, like users, products, and sales. 

Fast forward to today: Most computers run 64-bit Windows, and Microsoft Office has completely dropped support for these old Access 97 databases. This means companies with old data are completely locked out of their own files! 

## 🦸‍♂️ What Does AccessUtility Do?
**AccessUtility** is a rescue tool. 

Normally, to read an Access database, you need special "drivers" installed by Microsoft Office. **AccessUtility does not need them.** Instead, we built a custom engine in C# that reads the pure, raw 1s and 0s (binary data) of the `.mdb` file directly from the hard drive. 

Here is what the tool can do:
1. **Read Data**: It reads the tables and rows from the old database.
2. **Export Data**: It can convert the old data into modern formats like SQLite, PostgreSQL, or CSV so modern apps can use it.
3. **Fix Broken Files**: Old databases corrupt easily. This tool can repair broken pages and compact the database to make it smaller.
4. **Extract Files**: Sometimes people saved images or PDFs inside the database. We can extract them!
5. **Break Passwords**: If a company forgot their password from 25 years ago, we can decrypt it.

## 🧩 How is the Code Organized?
If you want to read the code, here is where to look:

- **`Models/`**: This folder contains the definitions of things. Think of them as blueprints (e.g., `AccessDatabase`, `AccessTable`, `AccessColumn`).
- **`Engine/`**: This is the heart of the project. This is where the actual "work" happens:
  - `Jet3BinaryReader.cs`: The magic file that reads the raw `.mdb` bytes.
  - `SchemaComparer.cs`: Looks at two databases and finds what changed.
  - `MaintenanceDaemon.cs`: A background worker that automatically cleans and backs up databases.
- **`AccessUtility.Tests/`**: This folder contains automated checks to make sure we don't break the code when we add new features.

## 🛠️ How Do I Run It?
This tool is a **Command-Line Interface (CLI)**. There are no buttons to click. You type commands into your terminal.

1. Open a terminal (like PowerShell or Command Prompt).
2. Type a command to run the tool. For example, to find out if a database has a password, you type:
   ```bash
   AccessUtility.exe password C:\MyOldData.mdb
   ```
3. The tool will print the answer directly in your terminal!

## 🎓 Next Steps for Beginners
Don't worry if it seems overwhelming! Here is what you should do next to learn:
1. **Play with it**: Build the code in Visual Studio or Rider, and try running the `AccessUtility.exe diagnose` command on a test `.mdb` file.
2. **Read the Next Guide**: Proceed to [01 - Introduction & Architecture](01-introduction-and-architecture.md) for a slightly deeper dive into how the binary data is read.
3. **Ask Questions**: We use an AI Assistant (AX) in the CLI! You can literally ask the tool how to use it by running: `AccessUtility.exe ax "How do I export data?"`

We are thrilled to have you here! Happy coding! 🎉
