'use strict';
const fs=require('node:fs');
const path=require('node:path');
function copy(source,destination){
 if(fs.statSync(source).isDirectory()){
  fs.mkdirSync(destination,{recursive:true});
  for(const name of fs.readdirSync(source)) copy(path.join(source,name),path.join(destination,name));
 }else fs.copyFileSync(source,destination);
}
(async()=>{
 const stage=path.join(__dirname,'.staging');
 fs.mkdirSync(stage,{recursive:true});
 for(const name of ['main.cjs','preload.cjs','controller.cjs','ui','native']) copy(path.join(__dirname,name),path.join(stage,name));
 copy(path.join(__dirname,'../shared'),path.join(stage,'shared'));
 const source=require('./package.json');
 fs.writeFileSync(path.join(stage,'package.json'),JSON.stringify({name:source.name,version:source.version,description:source.description,author:source.author,main:source.main},null,2));
 if(process.argv.includes('--prepare')) return;
 const {packager}=await import('@electron/packager');
 const results=await packager({dir:stage,out:path.join(__dirname,'release'),name:'PetbirdsDesktop',platform:'win32',arch:'x64',electronVersion:source.devDependencies.electron,overwrite:true,asar:false,prune:true,win32metadata:{CompanyName:'banirabbit',FileDescription:'Petbirds 桌面互动',ProductName:'Petbirds Desktop'}});
 for(const result of results) console.log(result);
})().catch(error=>{console.error(error);process.exitCode=1;});
